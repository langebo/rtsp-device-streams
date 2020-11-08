using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using CommandLine;
using DeviceProxyClient.Settings;
using DeviceProxyClient.CLI.Misc;

namespace DeviceProxyClient.CLI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Increase the connection limit, since the default for console apps is 2 
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;

            // Setting up application lifetime handling
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) => cts.Cancel();

            // Parsing startup parameters and running the app

            await Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsedAsync(options => RunAsync(options, cts.Token));
        }

        /// <summary>
        /// Configures and runs the proxy
        /// </summary>
        /// <param name="options">The parsed CLI arguments</param>
        /// <param name="cancellationToken">Token used for cancelling this operation</param>
        /// <returns>An awaitable async task</returns>
        private static async Task RunAsync(CommandLineOptions options, CancellationToken cancellationToken)
        {
            // Creating proxy settings instance
            var settings = new ProxyClientSettings
            {
                DeviceId = options.DeviceId,
                LocalPort = options.Port,
                BufferSize = options.BufferSize
            };

            // Create typed logger instance (usually done by DI)
            var logger = CommandLineLoggerFactory.CreateLogger<ProxyClient>(options.Verbosity);

            // Initializing the ServiceClient instance used to connect to the Azure IoT Hub
            using var serviceClient = ServiceClient.CreateFromConnectionString(options.ConnectionString, TransportType.Amqp);

            // Creating ProxyClient instance
            var proxyClient = new ProxyClient(serviceClient, settings, logger);

            // Starting the proxy client
            await proxyClient.ConnectProxyAsync(cancellationToken);
        }
    }
}