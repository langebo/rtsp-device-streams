using System;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using DeviceProxyClient.Settings;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Logging;

namespace DeviceProxyClient
{
    /// <summary>
    /// The ProxyClient class creates a TCP Listener. When a TCP connection is established the ProxyClient
    /// initiates the device stream via sending a request to the Azure IoT Hub. After the request is
    /// accepted it will create the WebSocket connection to the Streaming Hub and forward traffic between
    /// both connections.
    /// </summary>
    public class ProxyClient
    {
        private readonly ServiceClient serviceClient;
        private readonly ProxyClientSettings settings;
        private readonly ILogger logger;

        /// <summary>
        /// The constructor to create a new ProxyClient
        /// </summary>
        /// <param name="serviceClient">The ServiceClient used to connect to the Azure IoT Hub</param>
        /// <param name="settings">The ProxyClientSettings to configure the proxy client</param>
        /// <param name="logger">The ILogger instance to perform application logging</param>
        public ProxyClient(ServiceClient serviceClient, ProxyClientSettings settings, ILogger<ProxyClient> logger)
        {
            this.serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            if (string.IsNullOrEmpty(settings.DeviceId))
                throw new ArgumentException("DeviceId must not be null or empty", nameof(settings.DeviceId));

            if (settings.BufferSize < 8192)
                throw new ArgumentException("BufferSize must not be less than 8192 bytes", nameof(settings.BufferSize));

            if (settings.BufferSize > 65536)
                throw new ArgumentException("BufferSize must not be greater than 65536 bytes", nameof(settings.BufferSize));
        }

        /// <summary>
        /// Starts the local TCP listener, and starts listening for incoming connections
        /// </summary>
        /// <param name="cancellationToken">Token used for cancelling this operation.</param>
        /// <returns>An awaitable async task</returns>
        public async Task ConnectProxyAsync(CancellationToken cancellationToken)
        {
            var tcpListener = new TcpListener(IPAddress.Loopback, settings.LocalPort);
            tcpListener.Start();
            logger.LogInformation($"Started TCP listener on tcp://localhost:{(tcpListener.LocalEndpoint as IPEndPoint).Port}");

            while (!cancellationToken.IsCancellationRequested)
            {
                var tcpClient = await tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);

                logger.LogDebug("Local TCP connection established. Initiating proxy setup");
                _ = ConnectStreamsAsync(settings.DeviceId, tcpClient, cancellationToken);
            }
        }

        /// <summary>
        /// Initiates the device stream and maintains incoming and outgoing connections
        /// </summary>
        /// <param name="deviceId">The Id of the target device</param>
        /// <param name="tcpClient">The local TCP client</param>
        /// <param name="cancellationToken">Token used for cancelling this operation.</param>
        /// <returns>An awaitable async task</returns>
        private async Task ConnectStreamsAsync(string deviceId, TcpClient tcpClient, CancellationToken cancellationToken)
        {
            var request = new DeviceStreamRequest($"{deviceId}-{Guid.NewGuid()}");
            var result = await serviceClient.CreateStreamAsync(deviceId, request, cancellationToken).ConfigureAwait(false);
            if (!result.IsAccepted) return;

            logger.LogInformation($"Starting a proxy connection to device {deviceId}");

            using var localStream = tcpClient.GetStream();
            using var remoteStream = await GetStreamingClientAsync(result.Uri, result.AuthorizationToken, cancellationToken).ConfigureAwait(false);
            await Task.WhenAny(
                HandleIncomingDataAsync(localStream, remoteStream, cancellationToken),
                HandleOutgoingDataAsync(localStream, remoteStream, cancellationToken)).ConfigureAwait(false);

            logger.LogInformation($"Stopped a proxy connection with device {deviceId}");

            tcpClient.Close();
        }

        /// <summary>
        /// Reads incoming data from the remote WebSocket stream and writes it to the local TCP stream
        /// </summary>
        /// <param name="localStream">The local TCP stream</param>
        /// <param name="remoteStream">The remote streaming hub WebSocket stream</param>
        /// <param name="cancellationToken">Token used for cancelling this operation</param>
        /// <returns>An awaitable async task</returns>
        private async Task HandleIncomingDataAsync(NetworkStream localStream, ClientWebSocket remoteStream, CancellationToken cancellationToken)
        {
            logger.LogDebug($"Establishing incoming data pipe");

            byte[] receiveBuffer = new byte[settings.BufferSize];
            while (localStream.CanRead)
            {
                var receiveResult = await remoteStream.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken).ConfigureAwait(false);
                logger.LogTrace($"Forwading {receiveResult.Count} bytes from remote WebSocket connection");
                await localStream.WriteAsync(receiveBuffer, 0, receiveResult.Count).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Reads outgoing data from the local TCP stream and writes it to the streaming hub 
        /// </summary>
        /// <param name="localStream">The local TCP stream</param>
        /// <param name="remoteStream">The remote streaming hub WebSocket stream</param>
        /// <param name="cancellationToken">Token used for cancelling this operation</param>
        /// <returns>An awaitable async task</returns>
        private async Task HandleOutgoingDataAsync(NetworkStream localStream, ClientWebSocket remoteStream, CancellationToken cancellationToken)
        {
            logger.LogDebug($"Establishing outgoing data pipe");

            byte[] buffer = new byte[settings.BufferSize];
            while (remoteStream.State == WebSocketState.Open)
            {
                var receiveCount = await localStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                logger.LogTrace($"Forwading {receiveCount} bytes from local TCP connection");
                await remoteStream.SendAsync(new ArraySegment<byte>(buffer, 0, receiveCount), WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Creates a configured and connected ClientWebSocket
        /// </summary>
        /// <param name="uri">The WebSocket URI the client connects to</param>
        /// <param name="authToken">The bearer authentication token</param>
        /// <param name="cancellationToken">Token used for cancelling this operation</param>
        /// <returns>The configured and connected ClientWebSocket</returns>
        private async Task<ClientWebSocket> GetStreamingClientAsync(Uri uri, string authToken, CancellationToken cancellationToken)
        {
            var client = new ClientWebSocket();
            client.Options.SetRequestHeader("Authorization", $"Bearer {authToken}");

            await client.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
            return client;
        }
    }
}