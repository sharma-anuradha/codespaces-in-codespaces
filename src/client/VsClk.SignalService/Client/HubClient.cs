using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VsCloudKernel.SignalService.Common;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// Implements the IHubClient interface
    /// </summary>
    public class HubClient : IHubClient
    {
        private readonly TraceSource traceSource;
        private readonly CancellationTokenSource disposeCts = new CancellationTokenSource();

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
            this.traceSource = Requires.NotNull(trace, nameof(trace));
        }

        public HubConnection Connection { get; }

        public HubConnectionState State => Connection.State;
        public bool IsConnected => State == HubConnectionState.Connected;
        public bool IsRunning { get; private set; }

        public async Task DisposeAsync()
        {
            if (IsConnected)
            {
                await StopAsync(CancellationToken.None);
            }
            else
            {
                this.disposeCts.Cancel();
            }
        }

        public event AsyncEventHandler ConnectionStateChanged;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.traceSource.Verbose($"StartAsync");
            IsRunning = true;
            Connection.Closed += OnClosedAsync;
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, DisposeToken);
            return AttemptConnectAsync(cts.Token);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.traceSource.Verbose($"StopAsync");
            IsRunning = false;
            Connection.Closed -= OnClosedAsync;
            return Connection.StopAsync(cancellationToken);
        }

        private CancellationToken DisposeToken => this.disposeCts.Token;

        private async Task OnClosedAsync(Exception exception)
        {
            this.traceSource.Verbose($"OnClosedAsync exception:{exception?.Message}");
            await ConnectionStateChanged?.InvokeAsync(this, EventArgs.Empty);
            await AttemptConnectAsync(DisposeToken);
        }

        private async Task AttemptConnectAsync(CancellationToken cancellationToken)
        {
            await Connection.ConnectAsync(
                -1,
                5000,
                60000,
                this.traceSource,
                cancellationToken);
            await ConnectionStateChanged?.InvokeAsync(this, EventArgs.Empty);
        }
    }
}
