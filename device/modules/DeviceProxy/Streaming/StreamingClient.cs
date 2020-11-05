using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using DeviceProxy.Settings;
using System.Net.Sockets;

namespace DeviceProxy.Streaming
{
    public class StreamingClient
    {
        private readonly DeviceClient deviceClient;
        private readonly AppSettings settings;

        public StreamingClient(DeviceClient deviceClient, AppSettings settings)
        {
            this.deviceClient = deviceClient;
            this.settings = settings;
        }

        public async Task RunProxyAsync(CancellationToken cancellationToken)
        {
            var request = await deviceClient.WaitForDeviceStreamRequestAsync(cancellationToken).ConfigureAwait(false);
            if (request == null)
                return;

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

        private async Task HandleIncomingDataAsync(NetworkStream localStream, ClientWebSocket remoteStream, CancellationToken cancellationToken)
        {
            try
            {
                byte[] buffer = new byte[10240];
                while (remoteStream.State == WebSocketState.Open)
                {
                    // Receiving from streaming hub
                    var receiveResult = await remoteStream.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);

                    // Writing to application (rtsp server/camera)
                    await localStream.WriteAsync(buffer, 0, receiveResult.Count).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"INCOMING ERROR: {e.Message}");
                Console.WriteLine($"{e.StackTrace}");
            }
        }

        private async Task HandleOutgoingDataAsync(NetworkStream localStream, ClientWebSocket remoteStream, CancellationToken cancellationToken)
        {
            try
            {
                byte[] buffer = new byte[10240];
                while (localStream.CanRead)
                {
                    // Receiving from application (rtsp server/camera)
                    var receiveCount = await localStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

                    // Writing to streaming hub
                    await remoteStream.SendAsync(new ArraySegment<byte>(buffer, 0, receiveCount), WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"OUTGOING ERROR: {e.Message}");
                Console.WriteLine($"{e.StackTrace}");
            }
        }

        private async Task<ClientWebSocket> GetStreamingClientAsync(Uri uri, string authToken, CancellationToken cancellationToken)
        {
            var client = new ClientWebSocket();
            client.Options.SetRequestHeader("Authorization", $"Bearer {authToken}");

            await client.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
            return client;
        }
    }
}