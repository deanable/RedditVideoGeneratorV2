// RedditVideoGenerator.Core/Services/ConsoleLoggerService.cs
using System; // Required for Console and Exception

namespace RedditVideoGenerator.Core.Services
{
    public class ConsoleLoggerService : ILoggerService
    {
        private string GetFormattedMessage(string message, LogLevel level, Exception? ex = null)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string formattedMessage = $"[{timestamp}] [{level.ToString().ToUpperInvariant()}] {message}";
            if (ex != null)
            {
                formattedMessage += Environment.NewLine + "    Exception: " + ex.ToString();
            }
            return formattedMessage;
        }

        public void Log(string message, LogLevel level)
        {
            Console.WriteLine(GetFormattedMessage(message, level));
        }

        public void LogDebug(string message)
        {
            // For a simple console logger, we might only log debug messages if a specific flag is set.
            // For now, let's log it with a prefix.
            // In a more advanced setup, you'd use the Microsoft.Extensions.Logging framework directly here.
            Log(message, LogLevel.Debug);
        }

        public void LogInformation(string message)
        {
            Log(message, LogLevel.Information);
        }

        public void LogWarning(string message, Exception? ex = null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(GetFormattedMessage(message, LogLevel.Warning, ex));
            Console.ResetColor();
        }

        public void LogError(string message, Exception? ex = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(GetFormattedMessage(message, LogLevel.Error, ex));
            Console.ResetColor();
        }

        public void LogCritical(string message, Exception? ex = null)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(GetFormattedMessage(message, LogLevel.Critical, ex));
            Console.ResetColor();
        }
    }
}