using Microsoft.Extensions.Logging;

namespace DeviceProxyClient.CLI.Misc
{
    /// <summary>
    /// Static class for manually creating a console logger instance
    /// </summary>
    public static class CommandLineLoggerFactory
    {
        /// <summary>
        /// Creates a generic ILogger instance and configures it
        /// </summary>
        /// <param name="logLevel">Configures the the minimum log level</param>
        /// <returns>A generically typed ILogger instance</returns>
        public static ILogger<T> CreateLogger<T>(LogLevel logLevel)
        {
            return LoggerFactory
                .Create(l => l
                    .AddConsole(o => o.TimestampFormat = "[yyyy.MM.dd HH:mm:ss] ")
                    .SetMinimumLevel(logLevel))
                .CreateLogger<T>();
        }
    }
}