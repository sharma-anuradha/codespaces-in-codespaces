using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    /// <summary>
    /// Long running service that will accept json rpc connections
    /// </summary>
    public class JsonRpcServerService : BackgroundService
    {
        private const int JsonPort = 3150;

        public JsonRpcServerService(
            JsonRpcSessionManager JjonRpcSessionManager,
            ILogger<JsonRpcServerService> logger)
        {
            JsonRpcSessionManager = Requires.NotNull(JjonRpcSessionManager, nameof(JjonRpcSessionManager));
            Logger = Requires.NotNull(logger, nameof(logger));
        }

        private JsonRpcSessionManager JsonRpcSessionManager { get; }

        private ILogger<JsonRpcServerService> Logger { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var listener = CreateListener(JsonPort);
            listener.Start();

            while (true)
            {
                stoppingToken.ThrowIfCancellationRequested();

                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync();
                }
                catch (ObjectDisposedException)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    throw;
                }

                var tcpStream = client.GetStream();
                Logger.LogInformation(
                    $"Accepted incoming connection from {((IPEndPoint)client.Client.RemoteEndPoint).Address}");
                JsonRpcSessionManager.StartSession(tcpStream);
            }
        }

        /// <summary>
        /// Creates a TCP listener that listens on all local addresses for both IPv6 and IPv4.
        /// </summary>
        private static TcpListener CreateListener(int port)
        {
            try
            {
                var listener = new TcpListener(IPAddress.IPv6Any, port);
                listener.Server.SetSocketOption(
                    SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                return listener;
            }
            catch (SocketException)
            {
                // If IPV6 is turned off on the client machine, use IPV4 socket.
                var listener = new TcpListener(IPAddress.Any, port);
                return listener;
            }
        }

    }
}
