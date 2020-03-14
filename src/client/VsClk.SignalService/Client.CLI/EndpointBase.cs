// <copyright file="EndpointBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VsCloudKernel.SignalService.Client;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace SignalService.Client.CLI
{
    internal abstract class EndpointBase<T>
    {
        protected EndpointBase(HubClient hubClient, HubProxyOptions hubProxyOptions, TraceSource traceSource)
        {
            HubClient = hubClient;
            TraceSource = traceSource;
            Proxy = HubProxy.CreateHubProxy<T>(hubClient, traceSource, hubProxyOptions);
            HubClient.ConnectionStateChanged += OnConnectionStateChangedAsync;
        }

        public HubClient HubClient { get; private set; }

        public T Proxy { get; }

        public string ServiceId { get; private set; }

        public string Stamp { get; private set; }

        protected TraceSource TraceSource { get; }

        public static async Task<TInstance> CreateAsync<TInstance>(Func<HubClient, TInstance> instanceFactory, HubConnection hubConnection, TraceSource traceSource, CancellationToken cancellationToken)
            where TInstance : EndpointBase<T>
        {
            // try once
            var start = Stopwatch.StartNew();
            await hubConnection.StartAsync(cancellationToken);
            traceSource.Verbose($"hubConnection finished elapsed:{start.ElapsedMilliseconds}");

            var hubClient = new HubClient(hubConnection, traceSource);

            // next call will make sure the HubClient is in running state
            await hubClient.StartAsync(CancellationToken.None);

            var endpoint = instanceFactory(hubClient);
            await endpoint.RegisterAsync(cancellationToken);
            return endpoint;
        }

        public virtual async Task DisposeAsync()
        {
            (Proxy as IDisposable)?.Dispose();
            await HubClient.StopAsync(CancellationToken.None);
        }

        protected abstract Task<(string, string)> OnConnectedAsync(CancellationToken cancellationToken);

        private async Task OnConnectionStateChangedAsync(object sender, EventArgs args)
        {
            if (HubClient.State == HubConnectionState.Connected)
            {
                await RegisterAsync(CancellationToken.None);
            }
        }

        private async Task RegisterAsync(CancellationToken cancellationToken)
        {
            var serviceInfo = await OnConnectedAsync(cancellationToken);
            ServiceId = serviceInfo.Item1;
            Stamp = serviceInfo.Item2;
        }
    }
}
