// <copyright file="SignalRApp.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Microsoft.VsCloudKernel.SignalService.Client;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace SignalService.Client.CLI
{
    /// <summary>
    /// Base signalR app class that creates a single hub connection.
    /// </summary>
    internal abstract class SignalRApp : SignalRAppBase
    {
        protected HubClient HubClient { get; private set; }

        protected override Task OnStartedAsync()
        {
            HubClient = new HubClient(CreateHubConnection(), TraceSource);

            var stopWatch = Stopwatch.StartNew();
            AsyncEventHandler handler = null;
            handler = (s, e) =>
            {
                TraceSource.Verbose($"Connected in:{stopWatch.ElapsedMilliseconds} (ms)");
                HubClient.ConnectionStateChanged -= handler;
                return Task.CompletedTask;
            };

            HubClient.ConnectionStateChanged += handler;
            HubClient.StartAsync(DisposeToken).Forget();

            OnHubCreated();
            return Task.CompletedTask;
        }

        protected abstract void OnHubCreated();

        protected override async Task DiposeAsync()
        {
            await HubClient.StopAsync(CancellationToken.None);
        }

        protected override bool CanProcessKey(char key)
        {
            if (!HubClient.IsConnected)
            {
                Console.WriteLine("Waiting for Connection...");
                return false;
            }

            return true;
        }
    }
}
