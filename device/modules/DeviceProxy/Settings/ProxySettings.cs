namespace DeviceProxy.Settings
{
    /// <summary>
    /// Simple POCO class to configure the proxy
    /// </summary>
    public class ProxySettings
    {
        /// <summary>
        /// The device connection string
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// The hostname or IP of the endpoint that is being proxied
        /// </summary>
        public string RemoteHost { get; set; }

        /// <summary>
        /// The port of the endpoint that is being proxied
        /// </summary>
        public int RemotePort { get; set; }

        /// <summary>
        /// The buffer size of incoming and outgoing pipes
        /// </summary>
        /// <remarks>Must be between 8192 and 65536 (Defaults to 16384)</remarks>
        public int BufferSize { get; set; } = 16384;
    }
}