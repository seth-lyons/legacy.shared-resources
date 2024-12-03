using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SharedResources
{
    public class SFTPClient : IDisposable
    {
        private bool disposedValue;

        private SftpClient _client { get; set; }
        private Stream _key { get; set; }

        private ConnectionInfo _connectionInfo { get; set; }
        private string _directory { get; set; }
        public SftpClient Client { get => _client; }

        public SFTPClient(ConnectionInfo connectionInfo, string directory = null) => Initialize(connectionInfo, directory);
        public SFTPClient(ServerSettings connectionInfo, string directory = null)
            => Initialize(connectionInfo.Host, connectionInfo.Username, connectionInfo.Password, connectionInfo.Base64PrivateKey, (connectionInfo.Port == default ? 22 : connectionInfo.Port), directory);
        public SFTPClient(string host, string username, string password, string base64PrivateKey = null, int port = 22, string directory = null)
            => Initialize(host, username, password, base64PrivateKey, port, directory);

        private void Initialize(string host, string username, string password, string base64PrivateKey = null, int port = 22, string directory = null)
        {
            if (string.IsNullOrWhiteSpace(base64PrivateKey))
                Initialize(new ConnectionInfo(host, port, username, new PasswordAuthenticationMethod(username, password)), directory);
            else
            {
                _key = new MemoryStream(Convert.FromBase64String(base64PrivateKey));
                Initialize(new ConnectionInfo(host, port, username, new PrivateKeyAuthenticationMethod(username, string.IsNullOrWhiteSpace(password) ? new PrivateKeyFile(_key) : new PrivateKeyFile(_key, password))), directory);
            }
        }

        private void Initialize(ConnectionInfo connectionInfo, string directory = null)
        {
            _connectionInfo = connectionInfo;
            _directory = directory?.Trim()?.TrimEnd('/');
            EnsureConnected();
        }

        private void EnsureConnected()
        {
            if (_client == null)
                _client = new SftpClient(_connectionInfo);
            if (!_client.IsConnected)
                _client.Connect();
            if (_directory != null && !(_client.WorkingDirectory.Equals(_directory, StringComparison.InvariantCultureIgnoreCase)))
                _client.ChangeDirectory(_directory);
        }

        public void ListDirectory(string directory = ".") => Console.WriteLine(string.Join(Environment.NewLine, GetDirectory(directory).Select(dir => dir.FullName)));
        public IEnumerable<SftpFile> GetDirectory(string directory = ".")
        {
            EnsureConnected();
            return _client.ListDirectory(directory);
        }

        public void ChangeDirectory(string directory = ".")
        {
            EnsureConnected();
            directory = directory?.Trim()?.TrimEnd('/');
            if (!string.IsNullOrWhiteSpace(directory))
            {
                if (!(_client.WorkingDirectory.Equals(directory, StringComparison.InvariantCultureIgnoreCase)))
                    _client.ChangeDirectory(directory);
                _directory = directory;
            }
        }

        public void CreateDirectory(string directory)
        {
            EnsureConnected();
            if (!_client.Exists(directory))
                _client.CreateDirectory(directory);
        }

        public IEnumerable<SftpFile> GetFiles(string directory = ".", bool recursive = true, bool ensureConnection = true)
        {
            if (ensureConnection)
                EnsureConnected();
            if (!recursive)
                return _client.ListDirectory(directory)?.Where(a => !a.IsDirectory);
            else
            {
                var files = new List<SftpFile>();
                var folderContent = _client.ListDirectory(directory);

                var folderFiles = folderContent?.Where(a => !a.IsDirectory);
                if (folderFiles?.Any() == true)
                    files.AddRange(folderFiles);

                var folderDirs = folderContent?.Where(a => a.IsDirectory);
                if (folderDirs?.Any() == true)
                {
                    foreach (var folderDir in folderDirs)
                    {
                        var ff = GetFiles(folderDir.FullName, true, false);
                        if (ff?.Any() == true)
                            files.AddRange(ff);
                    }
                }
                return files;
            }
        }

        public string GetFileContents(string filePath, bool nullOnNotFound = false)
        {
            try
            {
                EnsureConnected();
                using (var ms = new MemoryStream())
                {
                    _client.DownloadFile(filePath, ms);
                    ms.Position = 0;
                    using (var reader = new StreamReader(ms))
                        return reader.ReadToEnd();
                }
            }
            catch (Renci.SshNet.Common.SftpPathNotFoundException)
            {
                if (nullOnNotFound)
                    return null;
                throw;
            }
        }

        public byte[] DownloadFile(string filePath)
        {
            EnsureConnected();
            using (var memoryStream = new MemoryStream())
            {
                _client.DownloadFile(filePath, memoryStream);
                return memoryStream.ToArray();
            }
        }

        public Stream DownloadFileStream(string filePath)
        {
            EnsureConnected();
            var memoryStream = new MemoryStream();
            _client.DownloadFile(filePath, memoryStream);
            return memoryStream;
        }

        public void UploadFile(string fileContent, string filePath, bool overwrite = false)
        {
            using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent)))
                UploadFile(memoryStream, filePath, overwrite);
        }

        public void UploadFile(byte[] file, string filePath, bool overwrite = false)
        {
            using (var memoryStream = new MemoryStream(file))
                UploadFile(memoryStream, filePath, overwrite);
        }

        public void UploadFile(Stream file, string filePath, bool overwrite = false)
        {
            EnsureConnected();
            _client.UploadFile(file, filePath, overwrite);
        }

        public void MoveFile(string filePath, string destination)
        {
            EnsureConnected();
            var file = _client.Get(filePath);
            file.MoveTo(destination);
        }

        public void DeleteFile(string filePath)
        {
            EnsureConnected();
            _client.DeleteFile(filePath);
        }

        public string MoveFile(SftpFile file, string destination, ConflictBehavior conflictBehavior = ConflictBehavior.Exception)
        {
            EnsureConnected();
            if (_client.Exists(destination))
            {
                if (conflictBehavior == ConflictBehavior.Overwrite)
                    _client.Delete(destination);                
                else if (conflictBehavior == ConflictBehavior.Rename)
                {
                    var fileParts = Regex.Match(destination, @"(.*[\\/])(.*)(\..*)");
                    if (fileParts.Success)
                    {
                        var folder = fileParts.Groups[1].Value;
                        var fileName = fileParts.Groups[2].Value;
                        var extension = fileParts.Groups[3].Value;

                        var number = 1;

                        do { destination = $"{folder}{fileName} ({number++}){extension}"; }
                        while (_client.Exists(destination));
                    }
                    else throw new Renci.SshNet.Common.SshException($"Could not determine file parts for renaming: {destination}");
                }
                else throw new Renci.SshNet.Common.SshException($"File already exists at destination location: {destination}");
            } 
            file.MoveTo(destination);
            return destination;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_client?.IsConnected == true)
                        _client.Disconnect();
                    ((IDisposable)_client).Dispose();

                    if (_key != null)
                    {
                        _key.Dispose();
                        _key = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~SFTPOperator()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
