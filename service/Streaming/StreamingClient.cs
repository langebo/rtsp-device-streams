using System;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using DeviceProxyService.Settings;
using Microsoft.Azure.Devices;

namespace DeviceProxyService.Streaming
{
    public class StreamingClient
    {
        private readonly ServiceClient serviceClient;
        private readonly AppSettings settings;

        public StreamingClient(ServiceClient serviceClient, AppSettings settings)
        {
            this.serviceClient = serviceClient;
            this.settings = settings;
        }

        public async Task ConnectProxyAsync(CancellationToken cancellationToken)
        {
            var tcpListener = new TcpListener(IPAddress.Loopback, settings.LocalPort);
            tcpListener.Start();

            while (true)
            {
                var tcpClient = await tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
                _ = ConnectStreamsAsync(settings.DeviceId, tcpClient, cancellationToken);
            }
        }

        private async Task ConnectStreamsAsync(string deviceId, TcpClient tcpClient, CancellationToken cancellationToken)
        {
            var request = new DeviceStreamRequest("TestStream");

            using (var localStream = tcpClient.GetStream())
            {
                var result = await serviceClient.CreateStreamAsync(deviceId, request, cancellationToken).ConfigureAwait(false);

                if (!result.IsAccepted) return;

                using var remoteStream = await GetStreamingClientAsync(result.Url, result.AuthorizationToken, cancellationToken).ConfigureAwait(false);

                await Task.WhenAny(
                    HandleIncomingDataAsync(localStream, remoteStream, cancellationToken),
                    HandleOutgoingDataAsync(localStream, remoteStream, cancellationToken)).ConfigureAwait(false);
            }

            tcpClient.Close();
        }

        private async Task HandleIncomingDataAsync(NetworkStream localStream, ClientWebSocket remoteStream, CancellationToken cancellationToken)
        {
            try
            {
                byte[] receiveBuffer = new byte[10240];
                while (localStream.CanRead)
                {
                    // Receiving from streaming hub
                    var receiveResult = await remoteStream.ReceiveAsync(receiveBuffer, cancellationToken).ConfigureAwait(false);

                    // Writing to application (vlc)
                    await localStream.WriteAsync(receiveBuffer, 0, receiveResult.Count).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"INCOMING ERROR: {ex.Message}");
                Console.WriteLine($"{ex.StackTrace}");
            }
        }

        private async Task HandleOutgoingDataAsync(NetworkStream localStream, ClientWebSocket remoteStream, CancellationToken cancellationToken)
        {
            try
            {
                byte[] buffer = new byte[10240];
                while (remoteStream.State == WebSocketState.Open)
                {
                    // Receiving from application (vlc)
                    var receiveCount = await localStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

                    // Writing to streaming hub
                    await remoteStream.SendAsync(new ArraySegment<byte>(buffer, 0, receiveCount), WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OUTGOING ERROR: {ex.Message}");
                Console.WriteLine($"{ex.StackTrace}");
            }
        }

        private async Task<ClientWebSocket> GetStreamingClientAsync(Uri url, string authToken, CancellationToken cancellationToken)
        {
            var client = new ClientWebSocket();
            client.Options.SetRequestHeader("Authorization", $"Bearer {authToken}");

            await client.ConnectAsync(url, cancellationToken).ConfigureAwait(false);
            return client;
        }
    }
}