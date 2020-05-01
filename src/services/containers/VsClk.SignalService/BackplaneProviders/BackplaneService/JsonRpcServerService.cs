﻿// <copyright file="JsonRpcServerService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VsCloudKernel.SignalService;
using StreamJsonRpc;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    /// <summary>
    /// Long running service that will accept json rpc connections.
    /// </summary>
    public class JsonRpcServerService : BackgroundService
    {
        private const string RegisterServiceMethod = "RegisterService";

        private readonly IEnumerable<IJsonRpcSessionFactory> jsonRpcSessionFactories;
        private readonly IOptions<AppSettingsBase> appSettingsProvider;

        public JsonRpcServerService(
            IEnumerable<IJsonRpcSessionFactory> jsonRpcSessionFactories,
            IOptions<AppSettingsBase> appSettingsProvider,
            ILogger<JsonRpcServerService> logger)
        {
            this.jsonRpcSessionFactories = Requires.NotNull(jsonRpcSessionFactories, nameof(jsonRpcSessionFactories));
            this.appSettingsProvider = Requires.NotNull(appSettingsProvider, nameof(appSettingsProvider));
            Logger = Requires.NotNull(logger, nameof(logger));
        }

        private AppSettingsBase AppSettings => this.appSettingsProvider.Value;

        private ILogger Logger { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var listener = CreateListener(AppSettings.JsonRpcPort);
            listener.Start();
            Logger.LogInformation($"Listening json-rpc on port:{AppSettings.JsonRpcPort}");

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
                var jsonRpc = AppSettings.IsJsonRpcMessagePackEnabled ? CreateJsonRpcWithMessagePack(tcpStream) : new JsonRpc(tcpStream);

                Action<string, string> registerCallback = (serviceType, serviceId) =>
                {
                    var factory = this.jsonRpcSessionFactories.FirstOrDefault(f => f.ServiceType == serviceType);
                    if (factory == null)
                    {
                        throw new InvalidOperationException($"service type:{serviceType} not found on the registered factories");
                    }

                    factory.StartRpcSession(jsonRpc, serviceId);
                };

                jsonRpc.AddLocalRpcMethod(RegisterServiceMethod, registerCallback);
                jsonRpc.AllowModificationWhileListening = true;
                jsonRpc.StartListening();
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

        private static JsonRpc CreateJsonRpcWithMessagePack(Stream tcpStream)
        {
            var handler = new LengthHeaderMessageHandler(tcpStream, tcpStream, new MessagePackFormatter());
            return new JsonRpc(handler);
        }
    }
}
