using CommandLine;
using Microsoft.Extensions.Logging;

namespace DeviceProxyClient.CLI.Misc
{
    /// <summary>
    /// Simple POCO class used to parse proxy configuration from CLI arguments
    /// </summary>
    public class CommandLineOptions
    {
        [Option('c', "connection-string", Required = true, HelpText = "The service connection string of the Azure IoT Hub")]
        public string ConnectionString { get; set; }

        [Option('d', "device-id", Required = true, HelpText = "The device id of the Azure IoT Device.")]
        public string DeviceId { get; set; }

        [Option('p', "port", Required = false, Default = 0, HelpText = "The local port to open (0 for an OS assigned port number)")]
        public int Port { get; set; }

        [Option('b', "buffer-size", Required = false, Default = 16384, HelpText = "The buffer size of incoming and outgoing pipes (Must be between 8192 and 65536")]
        public int BufferSize { get; set; }

        [Option('v', "verbosity", Required = false, Default = LogLevel.Information, HelpText = "The logging level. Options: [Information, None, Debug, Trace]")]
        public LogLevel Verbosity { get; set; }
    }
}