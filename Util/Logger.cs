using System;
using Serilog;

namespace ForecastingModule.Utilities
{
    public sealed class Logger
    {
        private static readonly Lazy<Logger> _instance = new Lazy<Logger>(() => new Logger());

        private ILogger _logger;

        // Private constructor for Singleton
        private Logger()
        {
            // Configure the logger (adjust sinks as needed)
            _logger = new LoggerConfiguration()
                //.WriteTo.Console()
                .WriteTo.File("logs.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }

        // Public accessor for the singleton instance
        public static Logger Instance => _instance.Value;

        // Public methods to log messages
        public void LogInfo(string message) => _logger.Information(message);
        public void LogWarning(string message) => _logger.Warning(message);
        public void LogError(string message) => _logger.Error(message);
        public void LogDebug(string message) => _logger.Debug(message);
    }
}
