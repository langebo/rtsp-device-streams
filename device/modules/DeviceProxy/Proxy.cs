using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using DeviceProxy.Settings;
using Microsoft.Extensions.Logging;

namespace DeviceProxy
{
    /// <summary>
    /// The Proxy class constantly waits for incoming device stream requests, coming from the Azure IoT Hub. 
    /// When a request arrives, it is accepted and a WebSocket connection to the Streaming Hub is initiated.
    /// A TCP connection to the target endpoint will be established and the traffic between both connections
    /// will be forwarded.
    /// </summary>
    public class Proxy
    {
        private readonly DeviceClient deviceClient;
        private readonly ProxySettings settings;
        private readonly ILogger logger;

        /// <summary>
        /// The constructor to create a new Proxy
        /// </summary>
        /// <param name="deviceClient">The DeviceClient used to connect to the Azure IoT Hub</param>
        /// <param name="settings">The ProxySettings to configure the proxy</param>
        /// <param name="logger">The ILogger instance to perform application logging</param>
        public Proxy(DeviceClient deviceClient, ProxySettings settings, ILogger<Proxy> logger)
        {
            this.deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            if (string.IsNullOrEmpty(settings.RemoteHost))
                throw new ArgumentException("RemoteHost must not be null or empty", nameof(settings.RemoteHost));

            if (settings.BufferSize < 8192)
                throw new ArgumentException("BufferSize must not be less than 8192 bytes", nameof(settings.BufferSize));

            if (settings.BufferSize > 65536)
                throw new ArgumentException("BufferSize must not be greater than 65536 bytes", nameof(settings.BufferSize));
        }

        /// <summary>
        /// Starts listening for incoming device stream requests
        /// </summary>
        /// <param name="cancellationToken">Token used for cancelling this operation</param>
        /// <returns>An awaitable async task</returns>
        public async Task RunProxyAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting to listen for incoming device stream requests");
            while (!cancellationToken.IsCancellationRequested)
            {
                var request = await deviceClient.WaitForDeviceStreamRequestAsync(cancellationToken).ConfigureAwait(false);
                if (request == null) continue;

                logger.LogDebug($"Device stream request received, Initiating proxy setup");
                _ = InitiateProxyConnectionsAsync(request, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Accepts the device stream request, connects to the local proxy target and maintains incoming and outgoing connections
        /// </summary>
        /// <param name="request">The device stream request</param>
        /// <param name="cancellationToken">Token used for cancelling this operation</param>
        /// <returns>An awaitable async task</returns>
        private async Task InitiateProxyConnectionsAsync(DeviceStreamRequest request, CancellationToken cancellationToken)
        {
            try
            {
                logger.LogInformation($"Starting a proxy connection. RequestId: {request.RequestId} Name: {request.Name}");

                await deviceClient.AcceptDeviceStreamRequestAsync(request, cancellationToken).ConfigureAwait(false);
                using var streamClient = await GetStreamingClientAsync(request.Uri, request.AuthorizationToken, cancellationToken).ConfigureAwait(false);
                using (var tcpClient = new TcpClient())
                {
                    await tcpClient.ConnectAsync(settings.RemoteHost, settings.RemotePort).ConfigureAwait(false);
                    using var networkStream = tcpClient.GetStream();

                    await Task.WhenAny(
                        HandleIncomingDataAsync(networkStream, streamClient, cancellationToken),
                        HandleOutgoingDataAsync(networkStream, streamClient, cancellationToken)).ConfigureAwait(false);

                    networkStream.Close();
                }

                await streamClient.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                logger.LogInformation($"Stopped a proxy connection. RequestId: {request.RequestId} Name: {request.Name}");
            }
        }

        /// <summary>
        /// Reads incoming data from the remote WebSocket stream and wirtes it to the local TCP stream
        /// </summary>
        /// <param name="localStream">The local TCP stream</param>
        /// <param name="remoteStream">The remote streaming hub WebSocket stream</param>
        /// <param name="cancellationToken">Token used for cancelling this operation</param>
        /// <returns>An awaitable async task</returns>
        private async Task HandleIncomingDataAsync(NetworkStream localStream, ClientWebSocket remoteStream, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[settings.BufferSize];
            while (remoteStream.State == WebSocketState.Open)
            {
                var receiveResult = await remoteStream.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                logger.LogTrace($"Forwading {receiveResult.Count} bytes from remote WebSocket connection");
                await localStream.WriteAsync(buffer, 0, receiveResult.Count).ConfigureAwait(false);
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
            byte[] buffer = new byte[settings.BufferSize];
            while (localStream.CanRead)
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