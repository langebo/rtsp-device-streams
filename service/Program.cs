using System;
using System.Net;
using System.Threading.Tasks;
using DeviceProxyService.Settings;
using DeviceProxyService.Streaming;
using Microsoft.Azure.Devices;

namespace DeviceProxyService
{
    public class Program
    {
        private const string ConnectionString = "<SERVICE_CONNECTION_STRING>";
        private const string DeviceId = "<DEVICE_ID>";
        private const int LocalPort = 9090;


        public static async Task Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;

            var settings = new AppSettings
            {
                DeviceId = DeviceId,
                LocalPort = LocalPort
            };

            using var serviceClient = ServiceClient.CreateFromConnectionString(ConnectionString, TransportType.Amqp);
            var client = new StreamingClient(serviceClient, settings);

            await client.ConnectProxyAsync(default);
        }
    }
}
