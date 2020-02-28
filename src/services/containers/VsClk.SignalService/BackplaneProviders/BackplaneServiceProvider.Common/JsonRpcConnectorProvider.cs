// <copyright file="JsonRpcConnectorProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using StreamJsonRpc;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Implement IBackplaneConnectorProvider based on a json rpc channel.
    /// </summary>
    public class JsonRpcConnectorProvider : IBackplaneConnectorProvider
    {
        private readonly string host;
        private readonly int port;
        private readonly Dictionary<string, Delegate> targetHandlers = new Dictionary<string, Delegate>();
        private JsonRpc jsonRpc;

        public JsonRpcConnectorProvider(string host, int port, ILogger logger)
        {
            Requires.NotNullOrEmpty(host, nameof(host));
            this.host = host;
            this.port = port;
            Logger = logger;
        }

        /// <inheritdoc/>
        public event EventHandler Disconnected;

        /// <inheritdoc/>
        public bool IsConnected => this.jsonRpc != null;

        private ILogger Logger { get; }

        /// <inheritdoc/>
        public async Task AttemptConnectAsync(CancellationToken cancellationToken)
        {
            var retries = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    Logger.LogDebug($"ConnectAsync -> host:{this.host} port:{this.port} retries:{retries}");
                    var tcpStream = await ConnectAsync(this.host, this.port, null, cancellationToken);
                    Attach(tcpStream);

                    Logger.LogDebug($"Succesfully connected...");
                    break;
                }
                catch (Exception err)
                {
                    ++retries;
                    Logger.LogDebug($"Failed to connect-> name:{err.GetType().Name} err:{err.Message}");
                    await Task.Delay(2000, cancellationToken);
                }
            }
        }

        /// <inheritdoc/>
        public Task<TResult> InvokeAsync<TResult>(string targetName, object[] arguments, CancellationToken cancellationToken)
        {
            return this.jsonRpc.InvokeWithCancellationAsync<TResult>(targetName, arguments, cancellationToken);
        }

        /// <inheritdoc/>
        public Task SendAsync(string targetName, object[] arguments, CancellationToken cancellationToken)
        {
            return this.jsonRpc.NotifyAsync(targetName, arguments);
        }

        /// <inheritdoc/>
        public void AddTarget(string methodName, Delegate handler)
        {
            this.targetHandlers.Add(methodName, handler);
        }

        private static async Task<Stream> ConnectAsync(string host, int port, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            TcpClient client;
            if (IPAddress.TryParse(host, out IPAddress ipAddress))
            {
                client = new TcpClient(ipAddress.AddressFamily);
            }
            else
            {
                client = new TcpClient();
            }

            // We may expect immediate responses from each packet we send, so disable a delay when send or receive buffers are not full.
            // client.NoDelay = DisableTcpDelay;
            Action cancel = () =>
            {
                if (!client.Connected)
                {
                    // Close the underlying Socket, rather than closing the TcpClient.
                    // This avoids a bug in TcpClient where it can throw a
                    // NullReferenceException if it's closed too early.
                    client.Client.Close();
                }
            };

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(cancel);
            }

            var timeoutSource = new CancellationTokenSource();
            if (timeout.HasValue)
            {
                timeoutSource.Token.Register(cancel);
                timeoutSource.CancelAfter(timeout.Value);
            }

            try
            {
                if (ipAddress != null)
                {
                    await client.ConnectAsync(ipAddress, port);
                }
                else
                {
                    await client.ConnectAsync(host, port);
                }
            }
            catch (ObjectDisposedException)
            {
                if (timeoutSource.IsCancellationRequested)
                {
                    throw new TimeoutException();
                }

                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }

            return client.GetStream();
        }

        private static JsonRpc CreateJsonRpcWithMessagePack(Stream tcpStream)
        {
            var handler = new LengthHeaderMessageHandler(tcpStream, tcpStream, new MessagePackFormatter());
            return new JsonRpc(handler);
        }

        private void Attach(Stream tcpStream)
        {
            this.jsonRpc = CreateJsonRpcWithMessagePack(tcpStream);
            foreach (var kvp in this.targetHandlers)
            {
                this.jsonRpc.AddLocalRpcMethod(kvp.Key, kvp.Value);
            }

            this.jsonRpc.Disconnected += (s, e) =>
            {
                Logger.LogError(e.Exception, $"Disconnected reason:{e.Reason}");
                this.jsonRpc = null;
                Disconnected?.Invoke(this, EventArgs.Empty);
            };
            this.jsonRpc.StartListening();
        }
    }
}
