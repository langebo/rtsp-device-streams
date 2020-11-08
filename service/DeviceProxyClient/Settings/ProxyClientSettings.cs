namespace DeviceProxyClient.Settings
{
    /// <summary>
    /// Simple POCO class to configure the proxy client
    /// </summary>
    public class ProxyClientSettings
    {
        /// <summary>
        /// The device id the proxy will connect to
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// The local port number the proxy will listen to
        /// </summary>
        /// <remarks>Set to 0 for an OS assigned port number</remarks>
        public int LocalPort { get; set; }

        /// <summary>
        /// The buffer size of incoming and outgoing pipes
        /// </summary>
        /// <remarks>Must be between 8192 and 65536</remarks>
        public int BufferSize { get; set; }
    }
}