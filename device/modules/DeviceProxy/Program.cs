using System;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using DeviceProxy.Settings;
using DeviceProxy.Streaming;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;

namespace DeviceProxy
{
    public class Program
    {

        public static async Task Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();

            await Init(cts.Token);
        }

        private static async Task Init(CancellationToken cancellationToken)
        {
            var settings = GetAppSettings();
            using var deviceClient = DeviceClient.CreateFromConnectionString(settings.ConnectionString, TransportType.Amqp);
            var streamingClient = new StreamingClient(deviceClient, settings);

            await streamingClient.RunProxyAsync(cancellationToken);
        }

        private static AppSettings GetAppSettings()
        {
            var settings = new AppSettings();

            new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build().Bind(settings);

            return settings;
        }
    }
}