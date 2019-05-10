using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
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
            : this(FromUrl(url).Build(), trace)
        {
        }

        public HubClient(string url, string accessToken, TraceSource trace)
            : this(FromUrlAndAccessToken(url, accessToken).Build(), trace)
        {
        }

        public HubClient(string url, Func<string> accessTokenCallback, TraceSource trace)
            : this(FromUrlAndAccessToken(url, accessTokenCallback).Build(), trace)
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

        public static IHubConnectionBuilder FromUrl(string url)
        {
            Requires.NotNullOrEmpty(url, nameof(url));
            return new HubConnectionBuilder().WithUrl(url).AddNewtonsoftJsonProtocol();
        }

        public static IHubConnectionBuilder FromUrlAndAccessToken(string url, string accessToken)
        {
            Requires.NotNullOrEmpty(accessToken, nameof(accessToken));
            return FromUrlAndAccessToken(url, () => accessToken);
        }

        public static IHubConnectionBuilder FromUrlAndAccessToken(string url, Func<string> accessTokenCallback)
        {
            Requires.NotNullOrEmpty(url, nameof(url));
            Requires.NotNull(accessTokenCallback, nameof(accessTokenCallback));
            return new HubConnectionBuilder().WithUrl(url, options =>
            {
                options.AccessTokenProvider = () =>
                {
                    return Task.FromResult(accessTokenCallback());
                };
            }).AddNewtonsoftJsonProtocol();
        }

        private async Task OnClosedAsync(Exception exception)
        {
            this.traceSource.Verbose($"OnClosedAsync exception:{exception?.Message}");
            await ConnectionStateChanged?.InvokeAsync(this, EventArgs.Empty);
            await AttemptConnectAsync(DisposeToken);
        }

        private async Task AttemptConnectAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    this.traceSource.Verbose($"AttemptConnectAsync.StartAsync");
                    await Connection.StartAsync(cancellationToken);
                    this.traceSource.Verbose($"Succesfully connected...");
                    await ConnectionStateChanged?.InvokeAsync(this, EventArgs.Empty);
                    break;
                }
                catch(Exception err)
                {
                    this.traceSource.Error($"Failed to connect->err:{err.Message}");
                }

                await Task.Delay(5000, cancellationToken);
            }
        }
    }
}
