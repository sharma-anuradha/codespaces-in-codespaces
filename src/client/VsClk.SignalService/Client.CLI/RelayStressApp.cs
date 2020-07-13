// <copyright file="RelayStressApp.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualStudio.Threading;
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
        private int delaySend = 50;
        private bool useSameService = false;
        private string serviceEndpointUri;
        private string currentHubId;
        private int jsonRpcCount = 1000;
        private bool traceHubData;
        private bool useSequenceProxy;
        private int messageSizeInKilobytes = 32;

        public RelayStressApp(string serviceEndpointUri, bool traceHubData)
        {
            this.serviceEndpointUri = serviceEndpointUri;
            this.traceHubData = traceHubData;
        }

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
                Utils.ReadIntValue("Enter delay time:", ref this.delaySend);
                if (string.IsNullOrEmpty(this.serviceEndpointUri))
                {
                    Utils.ReadBoolValue("Use same service:", ref this.useSameService);
                }

                Utils.ReadBoolValue("Use sequence proxy:", ref this.useSequenceProxy);

                var hubId = Guid.NewGuid().ToString();
                var hubEndpointHost = await CreateRelayHubEndpointAsync(hubId, CreateHubConnection());
                RelayHubEndpoint hubEndpointClient = null;

                TraceSource.Info($"Creating client hub...");
                while (!DisposeToken.IsCancellationRequested)
                {
                    var hubEndpoint = await CreateRelayHubEndpointAsync(hubId, CreateHubConnection(this.serviceEndpointUri));
                    if (!string.IsNullOrEmpty(this.serviceEndpointUri) ||
                        (this.useSameService && hubEndpointHost.ServiceId == hubEndpoint.ServiceId) ||
                        (!this.useSameService && hubEndpointHost.ServiceId != hubEndpoint.ServiceId))
                    {
                        hubEndpointClient = hubEndpoint;
                        break;
                    }

                    await hubEndpoint.DisposeAsync();
                    await Task.Delay(100, DisposeToken);
                }

                var receiveDataInfoTask = ReceiveDataAsync(TraceSource, hubEndpointClient, this.useSequenceProxy, DisposeToken);
                await SendDataAsync(TraceSource, hubEndpointHost, this.sizeInKilobytes * 1024, this.chunkBytes, this.delaySend, DisposeToken);
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

                var hubEndpoint = await CreateRelayHubEndpointAsync(this.currentHubId, CreateHubConnection());
                try
                {
                    await SendDataAsync(TraceSource, hubEndpoint, this.sizeInKilobytes * 1024, this.chunkBytes, 50, DisposeToken);
                }
                finally
                {
                    await hubEndpoint.DisposeAsync();
                }
            }
            else if (key == 'r')
            {
                Utils.ReadStringValue("Hub id:", ref this.currentHubId);
                if (string.IsNullOrEmpty(this.currentHubId))
                {
                    Console.WriteLine("Please specify a non empty hub id");
                    return;
                }

                var hubEndpoint = await CreateRelayHubEndpointAsync(this.currentHubId, CreateHubConnection());
                try
                {
                    await ReceiveDataAsync(TraceSource, hubEndpoint, false, DisposeToken);
                }
                finally
                {
                    await hubEndpoint.DisposeAsync();
                }
            }
            else if (key == 'j')
            {
                Utils.ReadIntValue("Enter number of json-rpc calls:", ref this.jsonRpcCount);
                await TestJsonRpcPerfAsync(this.jsonRpcCount);
            }
            else if (key == 'x')
            {
                Utils.ReadStringValue("Hub id:", ref this.currentHubId);
                if (string.IsNullOrEmpty(this.currentHubId))
                {
                    Console.WriteLine("Please specify a non empty hub id");
                    return;
                }

                Utils.ReadIntValue("Enter size in KB:", ref this.messageSizeInKilobytes);
                var hubEndpoint = await CreateRelayHubEndpointAsync(this.currentHubId, CreateHubConnection());
                try
                {
                    const string TypeMessageSize = "messageSize";

                    var buffer = new byte[this.messageSizeInKilobytes * 1024];
                    new Random().NextBytes(buffer);

                    var receivedEvent = new AsyncAutoResetEvent();
                    hubEndpoint.RelayHubProxy.ReceiveData += (s, e) =>
                    {
                        if (e.Type == TypeMessageSize)
                        {
                            if (e.Data.SequenceEqual(buffer))
                            {
                                Console.WriteLine("passed buffer verification!");
                            }
                            else
                            {
                                Console.Error.WriteLine("failed buffer verification!");
                            }

                            receivedEvent.Set();
                        }
                    };
                    await hubEndpoint.RelayHubProxy.SendDataAsync(SendOption.None, null, TypeMessageSize, buffer, null, HubMethodOption.Invoke, DisposeToken);
                    Console.WriteLine("Waiting to receive the message...");
                    await receivedEvent.WaitAsync(DisposeToken);
                }
                catch (Exception err)
                {
                    Console.Error.WriteLine($"failed to send message with size:{this.messageSizeInKilobytes}. Err:{err}");
                }
                finally
                {
                    await hubEndpoint.DisposeAsync();
                }
            }
        }

        private static async Task SendDataAsync(TraceSource traceSource, RelayHubEndpoint relayHubEndpoint, long totalSize, int chunkSize, int delayMillisecs, CancellationToken cancellationToken)
        {
            int nextSequence = 0;

            Func<Action<BinaryWriter>, Task<int>> sendCallback = async (writer) =>
            {
                var bufStream = new MemoryStream();
                var binaryWriter = new BinaryWriter(bufStream);
                writer(binaryWriter);
                return await relayHubEndpoint.RelayHubProxy.SendDataAsync(
                    SendOption.ExcludeSelf,
                    null,
                    "rawData",
                    bufStream.GetBuffer(),
                    RelayHubMessageProperties.CreateMessageSequence(Interlocked.Increment(ref nextSequence)),
                    HubMethodOption.Invoke,
                    cancellationToken);
            };

            var totalChunks = totalSize / chunkSize;

            var uniqueId = await sendCallback((writer) => writer.Write(totalChunks));
            traceSource.Info($"Send header sequence ->{totalChunks} uniqueId:{uniqueId}");

            for (int sequence = 0; sequence < totalChunks; ++sequence)
            {
                cancellationToken.ThrowIfCancellationRequested();

                uniqueId = await sendCallback((writer) =>
                {
                    writer.Write(DateTime.UtcNow.Ticks);
                    writer.Write(sequence);
                    writer.Write(new byte[chunkSize]);
                });
                traceSource.Info($"Complete send sequence:{sequence} uniqueId:{uniqueId}");

                await Task.Delay(delayMillisecs, cancellationToken);
            }

            traceSource.Info($"Send finished -> total size:{totalSize} total chunks:{totalChunks}");
        }

        private static async Task<(DateTime, int)> ReceiveDataAsync(
            TraceSource traceSource,
            RelayHubEndpoint relayHubEndpoint,
            bool useSequenceProxy,
            CancellationToken cancellationToken)
        {
            long totalTimeTicks = 0;
            int totalChunks = -1;
            int receivedSequence = 0;
            int numOfSequenceFails = 0;

            IRelayDataHubProxy sequenceReceiver;
            if (useSequenceProxy)
            {
                sequenceReceiver = new SequenceRelayDataHubProxy(relayHubEndpoint.RelayHubProxy, (e) => true);
            }
            else
            {
                sequenceReceiver = relayHubEndpoint.RelayHubProxy;
            }

            sequenceReceiver.ReceiveData += (s, e) =>
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
                    var sequenceFail = receivedSequence != sequenceReceived;
                    traceSource.Info($"Received sequence:{sequenceReceived} expected:{receivedSequence} time:{time.TotalMilliseconds} {(sequenceFail ? "**" : string.Empty)}");
                    if (sequenceFail)
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
                    traceSource.Info($"Receive finished -> total time:{totalTime.Second}.{totalTime.Millisecond} numOfSequenceFails:{numOfSequenceFails} numOfEvents:{(useSequenceProxy ? ((SequenceRelayDataHubProxy)sequenceReceiver).TotalEvents : 0)}");

                    return (totalTime, numOfSequenceFails);
                }

                await Task.Delay(100, cancellationToken);
            }

            throw new InvalidOperationException();
        }

        private async Task<RelayHubEndpoint> CreateRelayHubEndpointAsync(string hubId, HubConnection hubConnection)
        {
            var hubEndpointHost = await RelayHubEndpoint.CreateAsync(hubId, hubConnection, HubProxyOptions, TraceSource, DisposeToken);
            if (this.traceHubData)
            {
                hubEndpointHost.RelayHubProxy.RelayServiceProxy.TraceHubData = true;
            }

            return hubEndpointHost;
        }

        private async Task TestJsonRpcPerfAsync(int count)
        {
            var hubId = Guid.NewGuid().ToString();
            var hubEndpointHost = await CreateRelayHubEndpointAsync(hubId, CreateHubConnection());
            var hubEndpointClient = await CreateRelayHubEndpointAsync(hubId, CreateHubConnection(this.serviceEndpointUri));

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
