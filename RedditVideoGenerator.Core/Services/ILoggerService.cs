// RedditVideoGenerator.Core/Services/ILoggerService.cs
namespace RedditVideoGenerator.Core.Services
{
    public enum LogLevel
    {
        Debug,
        Information,
        Warning,
        Error,
        Critical
    }

    public interface ILoggerService
    {
        void Log(string message, LogLevel level);
        void LogDebug(string message);
        void LogInformation(string message);
        void LogWarning(string message, Exception? ex = null);
        void LogError(string message, Exception? ex = null);
        void LogCritical(string message, Exception? ex = null);
    }
}