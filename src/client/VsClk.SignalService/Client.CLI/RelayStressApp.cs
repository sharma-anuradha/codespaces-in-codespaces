// <copyright file="RelayStressApp.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VsCloudKernel.SignalService;
using Microsoft.VsCloudKernel.SignalService.Client;
using Microsoft.VsCloudKernel.SignalService.Common;
using StreamJsonRpc;

namespace SignalService.Client.CLI
{
    internal class RelayStressApp : SignalRAppBase
    {
        private const string TypeJsonRpc = "json-rpc";
        private const string Method1 = "method1";

        private int sizeInKilobytes = 1000;
        private int chunkBytes = 2048;
        private bool useSameService = false;
        private string clientHubServiceUri;
        private string currentHubId;
        private int jsonRpcCount = 1000;

        protected override Task DiposeAsync()
        {
            return Task.CompletedTask;
        }

        protected override async Task HandleKeyAsync(char key)
        {
            if (key == 't')
            {
                Utils.ReadIntValue("Enter size in KB:", ref this.sizeInKilobytes);
                Utils.ReadIntValue("Enter chunk size:", ref this.chunkBytes);
                Utils.ReadBoolValue("Use same service:", ref this.useSameService);
                Utils.ReadStringValue("Client Service uri", ref this.clientHubServiceUri);

                var hubId = Guid.NewGuid().ToString();
                var hubEndpointHost = await RelayHubEndpoint.CreateAsync(hubId, CreateHubConnection(), HubProxyOptions, TraceSource, DisposeToken);
                RelayHubEndpoint hubEndpointClient = null;

                TraceSource.Info($"Creating client hub...");
                while (!DisposeToken.IsCancellationRequested)
                {
                    var hubEndpoint = await RelayHubEndpoint.CreateAsync(hubId, CreateHubConnection(this.clientHubServiceUri), HubProxyOptions, TraceSource, DisposeToken);
                    if ((this.useSameService && hubEndpointHost.ServiceId == hubEndpoint.ServiceId) ||
                        (!this.useSameService && hubEndpointHost.ServiceId != hubEndpoint.ServiceId))
                    {
                        hubEndpointClient = hubEndpoint;
                        break;
                    }

                    await hubEndpoint.DisposeAsync();
                    await Task.Delay(100, DisposeToken);
                }

                var receiveDataInfoTask = ReceiveDataAsync(TraceSource, hubEndpointClient, DisposeToken);
                await SendDataAsync(TraceSource, hubEndpointHost, this.sizeInKilobytes * 1024, this.chunkBytes, 50, DisposeToken);
                await receiveDataInfoTask;
                TraceSource.Info($"Host -> serviceId:{hubEndpointHost.ServiceId} stamp:{hubEndpointHost.Stamp}");
                TraceSource.Info($"Client -> serviceId:{hubEndpointClient.ServiceId} stamp:{hubEndpointClient.Stamp}");
                await hubEndpointHost.Proxy.DeleteHubAsync(hubId, DisposeToken);

                await hubEndpointHost.DisposeAsync();
                await hubEndpointClient.DisposeAsync();
            }
            else if (key == 's')
            {
                Utils.ReadStringValue("Hub id:", ref this.currentHubId);
                if (string.IsNullOrEmpty(this.currentHubId))
                {
                    Console.WriteLine("Please specify a non empty hub id");
                    return;
                }

                Utils.ReadIntValue("Enter size in KB:", ref this.sizeInKilobytes);
                Utils.ReadIntValue("Enter chunk size:", ref this.chunkBytes);

                var hubEndpoint = await RelayHubEndpoint.CreateAsync(this.currentHubId, CreateHubConnection(), HubProxyOptions, TraceSource, DisposeToken);
                await SendDataAsync(TraceSource, hubEndpoint, this.sizeInKilobytes * 1024, this.chunkBytes, 50, DisposeToken);
            }
            else if (key == 'r')
            {
                Utils.ReadStringValue("Hub id:", ref this.currentHubId);
                if (string.IsNullOrEmpty(this.currentHubId))
                {
                    Console.WriteLine("Please specify a non empty hub id");
                    return;
                }

                var hubEndpoint = await RelayHubEndpoint.CreateAsync(this.currentHubId, CreateHubConnection(), HubProxyOptions, TraceSource, DisposeToken);
                await ReceiveDataAsync(TraceSource, hubEndpoint, DisposeToken);
            }
            else if (key == 'j')
            {
                Utils.ReadIntValue("Enter number of json-rpc calls:", ref this.jsonRpcCount);
                await TestJsonRpcPerfAsync(this.jsonRpcCount);
            }
        }

        private static async Task SendDataAsync(TraceSource traceSource, RelayHubEndpoint relayHubEndpoint, long totalSize, int chunkSize, int delayMillisecs, CancellationToken cancellationToken)
        {
            Func<Action<BinaryWriter>, Task> sendCallback = async (writer) =>
            {
                var bufStream = new MemoryStream();
                var binaryWriter = new BinaryWriter(bufStream);
                writer(binaryWriter);
                await relayHubEndpoint.RelayHubProxy.SendDataAsync(SendOption.ExcludeSelf, null, "rawData", bufStream.GetBuffer(), cancellationToken);
            };

            var totalChunks = totalSize / chunkSize;

            traceSource.Info($"Send header sequence ->{totalChunks}");
            await sendCallback((writer) => writer.Write(totalChunks));

            for (int sequence = 0; sequence < totalChunks; ++sequence)
            {
                cancellationToken.ThrowIfCancellationRequested();

                traceSource.Info($"Sending sequence:{sequence}");
                await sendCallback((writer) =>
                {
                    writer.Write(DateTime.UtcNow.Ticks);
                    writer.Write(sequence);
                    writer.Write(new byte[chunkSize]);
                });
                await Task.Delay(delayMillisecs, cancellationToken);
            }

            traceSource.Info($"Send finished -> total size:{totalSize} total chunks:{totalChunks}");
        }

        private static async Task<(DateTime, int)> ReceiveDataAsync(TraceSource traceSource, RelayHubEndpoint relayHubEndpoint, CancellationToken cancellationToken)
        {
            long totalTimeTicks = 0;
            int totalChunks = -1;
            int receivedSequence = 0;
            int numOfSequenceFails = 0;

            relayHubEndpoint.RelayHubProxy.ReceiveData += (s, e) =>
            {
                var bufStream = new MemoryStream(e.Data);
                var binaryReader = new BinaryReader(bufStream);

                if (totalChunks == -1)
                {
                    totalChunks = binaryReader.ReadInt32();
                    traceSource.Info($"ready to receive number of chunks:{totalChunks}");
                }
                else
                {
                    var utcTicks = binaryReader.ReadInt64();
                    var utcTimeSend = new DateTime(utcTicks);
                    var utcNow = DateTime.UtcNow;
                    TimeSpan time;
                    if (utcNow > utcTimeSend)
                    {
                        time = utcNow.Subtract(utcTimeSend);
                    }
                    else
                    {
                        // Note: this could happen when there are secs difference on two machines
                        time = TimeSpan.Zero;
                    }

                    var sequenceReceived = binaryReader.ReadInt32();
                    totalTimeTicks += time.Ticks;
                    traceSource.Info($"Received sequence:{sequenceReceived} expected:{receivedSequence} time:{time.TotalMilliseconds}");
                    if (receivedSequence != sequenceReceived)
                    {
                        ++numOfSequenceFails;
                    }

                    ++receivedSequence;
                }
            };

            while (!cancellationToken.IsCancellationRequested)
            {
                if (totalChunks != -1 && receivedSequence >= totalChunks)
                {
                    var totalTime = new DateTime(totalTimeTicks);
                    traceSource.Info($"Receive finished -> total time:{totalTime.Second}.{totalTime.Millisecond} numOfSequenceFails:{numOfSequenceFails}");

                    return (totalTime, numOfSequenceFails);
                }

                await Task.Delay(100, cancellationToken);
            }

            throw new InvalidOperationException();
        }

        private async Task TestJsonRpcPerfAsync(int count)
        {
            var hubId = Guid.NewGuid().ToString();
            var hubEndpointHost = await RelayHubEndpoint.CreateAsync(hubId, CreateHubConnection(), HubProxyOptions, TraceSource, DisposeToken);
            var hubEndpointClient = await RelayHubEndpoint.CreateAsync(hubId, CreateHubConnection(), HubProxyOptions, TraceSource, DisposeToken);

            var hostStream = new RelayHubStream(hubEndpointHost.RelayHubProxy, hubEndpointClient.RelayHubProxy.SelfParticipant.Id, TypeJsonRpc);
            var jsonHostRpc = new JsonRpc(hostStream);
            Func<int, string, string> method1 = (value, str) =>
            {
                return $"value:{value} str:{str}";
            };
            jsonHostRpc.AddLocalRpcMethod(Method1, method1);
            jsonHostRpc.StartListening();

            var clientStream = new RelayHubStream(hubEndpointClient.RelayHubProxy, hubEndpointHost.RelayHubProxy.SelfParticipant.Id, TypeJsonRpc);
            var jsonClientRpc = new JsonRpc(clientStream);
            jsonClientRpc.StartListening();

            var elapsedTimes = new List<TimeSpan>();
            var sb = new StringBuilder();

            long totalTimeTicks = 0;
            for (int i = 0; i < count; ++i)
            {
                var start = DateTime.UtcNow;
                var result = await jsonClientRpc.InvokeAsync<string>(Method1, 100, "Hello");

                var elasped = DateTime.UtcNow.Subtract(start);
                elapsedTimes.Add(elasped);
                sb.Append($"{elasped.Milliseconds}-");

                Console.WriteLine($"json-rpc elapsed ms:{elasped.Milliseconds}");
                totalTimeTicks += elasped.Ticks;
                await Task.Delay(100, DisposeToken);
            }

            Console.WriteLine($"elapsed times:{sb}");
            var averageElapsed = new DateTime(totalTimeTicks / count);
            Console.WriteLine($"Average json-rpc:{averageElapsed.Millisecond}");

            await hubEndpointHost.RelayHubProxy.RelayServiceProxy.DeleteHubAsync(hubId, DisposeToken);
            await hubEndpointHost.DisposeAsync();
            await hubEndpointClient.DisposeAsync();
        }

        private class RelayHubEndpoint : EndpointBase<RelayServiceProxy>
        {
            internal RelayHubEndpoint(string hubId, HubClient hubClient, HubProxyOptions hubProxyOptions, TraceSource traceSource)
                : base(hubClient, hubProxyOptions, traceSource)
            {
                HubId = hubId;
            }

            public string HubId { get; }

            public IRelayHubProxy RelayHubProxy { get; private set; }

            public static async Task<RelayHubEndpoint> CreateAsync(string hubId, HubConnection hubConnection, HubProxyOptions hubProxyOptions, TraceSource traceSource, CancellationToken cancellationToken)
            {
                traceSource.Verbose($"Creating endpoint for hub id:{hubId}");

                return await CreateAsync(
                    (hubClient) => new RelayHubEndpoint(hubId, hubClient, hubProxyOptions, traceSource),
                    hubConnection,
                    traceSource,
                    cancellationToken);
            }

            protected override async Task<(string, string)> OnConnectedAsync(CancellationToken cancellationToken)
            {
                RelayHubProxy = await Proxy.JoinHubAsync(HubId, null, new JoinOptions() { CreateIfNotExists = true }, cancellationToken);
                var serviceId = RelayHubProxy.ServiceId;
                var stamp = RelayHubProxy.Stamp;
                TraceSource.Verbose($"join completed for hub id:{HubId} on serviceId:{serviceId} stamp:{stamp}");
                return (serviceId, stamp);
            }
        }
    }
}
