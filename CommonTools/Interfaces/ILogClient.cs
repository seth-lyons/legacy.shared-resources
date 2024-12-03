
namespace SharedResources
{
    public interface ILogClient 
    {
        void Info(string message);
        void Warning(string message);
        void Error(string message);
        void Add(string message, LogType logType);
    }
}
