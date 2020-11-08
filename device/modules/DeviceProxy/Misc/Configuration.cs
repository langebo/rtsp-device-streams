using System;
using DeviceProxy.Settings;
using Microsoft.Extensions.Configuration;

namespace DeviceProxy.Misc
{
    /// <summary>
    /// Static class to retrieve application configuration and settings
    /// </summary>
    public static class Configuration
    {
        /// <summary>
        /// Creates an IConfiguration instance from configuration files and environment variables
        /// </summary>
        /// <returns>A populated instance of IConfiguration</returns>
        public static IConfiguration GetConfiguration()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{environment}.json", true)
                .AddEnvironmentVariables()
                .Build();
        }

        /// <summary>
        /// Retrieves proxy settings from configuration
        /// </summary>
        /// <param name="config">The IConfiguration instance used to configure the ProxySettings</param>
        /// <returns>A populated instance of ProxySettings</returns>
        public static ProxySettings GetProxySettings(IConfiguration config)
        {
            var settings = new ProxySettings();
            config.Bind(settings);
            return settings;
        }
    }
}