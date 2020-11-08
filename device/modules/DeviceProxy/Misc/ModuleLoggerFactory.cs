using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DeviceProxy.Misc
{
    /// <summary>
    /// Static class for manually creating a console logger instance
    /// </summary>
    public static class ModuleLoggingFactory
    {
        /// <summary>
        /// Creates a generic ILogger instance and configures it
        /// </summary>
        /// <param name="config">The IConfiguration used to configure the logger</param>
        /// <returns>A generically typed ILogger instance</returns>
        public static ILogger<T> CreateLogger<T>(IConfiguration config)
        {
            return LoggerFactory
                .Create(l => l
                    .AddConfiguration(config)
                    .AddConsole(o => o.TimestampFormat = "[yyyy.MM.dd HH:mm:ss] "))
                .CreateLogger<T>();
        }
    }
}