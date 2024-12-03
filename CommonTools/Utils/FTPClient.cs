using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SharedResources
{
    public class FTPClient : IDisposable
    {
        private WebClient _client { get; set; }
        private ServerSettings _connectionInfo { get; set; }
        private string _directory { get; set; }


        public FTPClient(ServerSettings connectionInfo, string directory = null)
            => Initialize(connectionInfo, directory);

        public FTPClient(string host, string username, string password, int port = 22, string directory = null)
            => Initialize(new ServerSettings { Host = host, Username = username, Password = password, Port = port }, directory);

        private void Initialize(ServerSettings connectionInfo, string directory = null)
        {
            _connectionInfo = connectionInfo;
            _directory = directory?.Trim()?.TrimEnd('/');
            _client = new WebClient();
            _client.Credentials = new NetworkCredential(connectionInfo.Username, connectionInfo.Password);
            _client.BaseAddress = _client.BaseAddress;
        }

        public List<string> ListDirectory(string folderPath)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(folderPath);
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.Credentials = _client.Credentials;
            request.KeepAlive = false;
            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream))
                return reader.ReadToEnd().Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
        }


        public void UploadFile(string filePath, byte[] fileContent)
        {
            using (var postStream = _client.OpenWrite(filePath))
                postStream.Write(fileContent, 0, fileContent.Length);
        }

        public void Dispose()
        {
            ((IDisposable)_client).Dispose();
        }
    }
}
