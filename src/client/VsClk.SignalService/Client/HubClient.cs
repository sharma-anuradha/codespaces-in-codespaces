// <copyright file="HubClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualStudio.Threading;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// Implements the IHubClient interface.
    /// </summary>
    public class HubClient : IHubClient
    {
        private readonly TraceSource traceSource;
        private CancellationTokenSource stopCts;

        public HubClient(string url, TraceSource trace)
            : this(HubConnectionHelpers.FromUrl(url).Build(), trace)
        {
        }

        public HubClient(string url, string accessToken, TraceSource trace)
            : this(HubConnectionHelpers.FromUrlAndAccessToken(url, accessToken).Build(), trace)
        {
        }

        public HubClient(string url, Func<string> accessTokenCallback, TraceSource trace)
            : this(HubConnectionHelpers.FromUrlAndAccessToken(url, accessTokenCallback).Build(), trace)
        {
        }

        public HubClient(HubConnection hubConnection, TraceSource trace)
        {
            Connection = Requires.NotNull(hubConnection, nameof(trace));
            Connection.Closed += OnClosedAsync;
            this.traceSource = Requires.NotNull(trace, nameof(trace));
        }

        /// <inheritdoc/>
        public event AsyncEventHandler ConnectionStateChanged;

        /// <inheritdoc/>
        public event AsyncEventHandler<AttemptConnectionEventArgs> AttemptConnection;

        /// <summary>
        /// Gets underlying hub connection.
        /// </summary>
        public HubConnection Connection { get; }

        /// <inheritdoc/>
        public HubConnectionState State => Connection.State;

        /// <inheritdoc/>
        public bool IsConnected => State == HubConnectionState.Connected;

        /// <inheritdoc/>
        public bool IsRunning { get; private set; }

        private CancellationToken StopToken => this.stopCts.Token;

        /// <inheritdoc/>
        public Task DisposeAsync()
        {
            return StopAsync(CancellationToken.None);
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!IsRunning)
            {
                this.traceSource.Verbose($"StartAsync");
                IsRunning = true;
                this.stopCts?.Dispose();
                this.stopCts = new CancellationTokenSource();
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, StopToken);
                return AttemptConnectAsync(cts.Token);
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (IsRunning)
            {
                this.traceSource.Verbose($"StopAsync");
                IsRunning = false;
                this.stopCts.Cancel();
                return Connection.StopAsync(cancellationToken);
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        private async Task OnClosedAsync(Exception exception)
        {
            this.traceSource.Verbose($"OnClosedAsync exception:{exception?.Message}");
            await FireConnectionStateChangedAsync();

            if (IsRunning)
            {
                AttemptConnectAsync(StopToken).Forget();
            }
        }

        private async Task FireConnectionStateChangedAsync()
        {
            if (ConnectionStateChanged != null)
            {
                await ConnectionStateChanged.InvokeAsync(this, EventArgs.Empty);
            }
        }

        private async Task AttemptConnectAsync(CancellationToken cancellationToken)
        {
            await Connection.ConnectAsync(
                async (retries, backoffTime, error) =>
                {
                    var e = new AttemptConnectionEventArgs(retries, backoffTime, error);
                    if (AttemptConnection != null)
                    {
                        await AttemptConnection.InvokeAsync(this, e);
                    }

                    return e.BackoffTimeMillisecs;
                },
                -1,
                5000,
                60000,
                this.traceSource,
                cancellationToken);
            await FireConnectionStateChangedAsync();
        }
    }
}
