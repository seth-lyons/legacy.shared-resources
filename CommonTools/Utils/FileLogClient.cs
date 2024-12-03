using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SharedResources
{
    public enum LogType
    {
        Information = 0,
        Error = 1,
        Warning = 2
    }

    public class FileLogClient
    {
        private readonly string _path;

        public FileLogClient(string fileName = "log.txt", string directory = null)
        {
            if (directory == null) directory = $@"C:\ProgramData\Nations Lending Corporation\Logs\{GetCurrentNamespace()}\";
            Directory.CreateDirectory(directory);
            _path = Path.Combine(directory, fileName);
        }

        public void AddCollection(IEnumerable<string> text, bool appendNewline = true, bool reorder = true)
        {
            if (reorder)
                text = text.OrderBy(a => a);
            Write(string.Join(appendNewline ? Environment.NewLine : "", text), null, false);
        }
        public void Add(string message, LogType logType) => Write(message, logType, true);
        public void AddNoPrefix(string message) => Write(message, null, false);

        public void Info(string message) => Write(message, LogType.Information, true);
        public void Warning(string message) => Write(message, LogType.Warning, true);
        public void Error(string message) => Write(message, LogType.Error, true);
        public void NewLine() => Write(string.Empty, null, false);

        private void Write(string message, LogType? logType, bool usePrefix = true, int retry = 3)
        {
            try
            {
                File.AppendAllText(_path, $"{(usePrefix ? GetPrefix(logType) : "")}{message}{Environment.NewLine}");
            }
            catch
            {
                if (retry > 0)
                    Write(message, logType, usePrefix, --retry);
            }
        }

        private static string GetCurrentNamespace()
        {
            return System.Reflection.Assembly.GetEntryAssembly()?.GetName()?.Name ??
                System.Reflection.Assembly.GetCallingAssembly()?.GetName()?.Name ??
                System.Reflection.Assembly.GetExecutingAssembly()?.GetName()?.Name;
        }

        public static string GetPrefixedMessage(string message, LogType? logType, bool appendNewline = false) =>
            $"{GetPrefix(logType)}{message}{(appendNewline ? Environment.NewLine : "")}";

        public static string GetPrefix(LogType? logType)
        {
            return $"<{DateTime.Now:yyyy-MM-dd HH:mm:ss}> " +
                    (logType == LogType.Error ? "[ERROR] " :
                    logType == LogType.Warning ? "[WARNING] " :
                    logType == LogType.Information ? "[INFO] " : "");
        }
    }
}
