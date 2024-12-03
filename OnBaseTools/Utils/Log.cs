using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SharedResources
{
    public class FileLogger
    {
        private string _filePrefix;
        private readonly string _directory;

        public FileLogger(string pluginName, string fileName = "log")
        {
            _directory = $@"C:\ProgramData\OnBase\Logs\{pluginName}\";
            Directory.CreateDirectory(_directory);
            _filePrefix = fileName;
        }

        public void SetFileName(string fileName = "log") => _filePrefix = fileName; 

        public void AddCollection(string text) => Write(text, null, false);
        public void AddCollection(IEnumerable<string> text, bool appendNewline = true, bool reorder = true)
        {
            if (reorder)
                text = text.OrderBy(a => a);
            Write(string.Join(appendNewline ? Environment.NewLine : "", text), null, false);
        }
        public void Add(string message, LogType logType) => Write(message, logType, true);

        public void Info(string message) => Write(message, LogType.Information, true);
        public void Warning(string message) => Write(message, LogType.Warning, true);
        public void Error(string message) => Write(message, LogType.Error, true);
        public void NewLine() => Write(string.Empty, null, false);

        private void Write(string message, LogType? logType, bool usePrefix = true, int retry = 3)
        {
            try
            {
                var textToWrite = $"{(usePrefix ? GetPrefix(logType) : "")}{message}{Environment.NewLine}";
                File.AppendAllText(Path.Combine(_directory, $"{_filePrefix}_{DateTime.Today:MM-dd-yyyy}.txt"), textToWrite);
            }
            catch (Exception e)
            {
                if (retry > 0)
                    Write(message, logType, usePrefix, --retry);
            }
        }
        public string GetDirectory => _directory;

        public static string GetPrefixedMessage(string message, LogType? logType, bool appendNewline = false) =>
            $"{GetPrefix(logType)}{message}{(appendNewline ? Environment.NewLine : "")}";

        public static string GetPrefix(LogType? logType)
        {
            return $"<{DateTime.Now.ToString("HH:mm:ss")}> " +
                    (logType == LogType.Error ? "[ERROR] " :
                    logType == LogType.Warning ? "[WARNING] " :
                    logType == LogType.Information ? "[INFO] " : "");
        }

    }
}
