using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using DeviceProxy.Misc;
using Microsoft.Azure.Devices.Client;

namespace DeviceProxy
{
    public class Program
    {

        public static async Task Main(string[] args)
        {
            // Configure application lifetime handling 
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();

            await Init(cts.Token);
        }

        /// <summary>
        /// Initializes the device proxy module
        /// </summary>
        /// <param name="cancellationToken">Token used for cancelling this operation</param>
        /// <returns>An awaitable async task</returns>
        private static async Task Init(CancellationToken cancellationToken)
        {
            // Retrieve the configuration
            var config = Configuration.GetConfiguration();

            // Retrieving proxy settings
            var settings = Configuration.GetProxySettings(config);

            // Create typed logger instance
            var logger = ModuleLoggingFactory.CreateLogger<Proxy>(config);

            // Initiating the DeviceClient instace used to connect to the Azure IoT Hub
            using var deviceClient = DeviceClient.CreateFromConnectionString(settings.ConnectionString, TransportType.Amqp);

            // Creating the Proxy instance
            var proxy = new Proxy(deviceClient, settings, logger);

            // Starting the proxy instance
            await proxy.RunProxyAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}