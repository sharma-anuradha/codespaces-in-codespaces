using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VsCloudKernel.SignalService.Common;
#if SignalR_Client_Version3
using Microsoft.Extensions.DependencyInjection;
#endif

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// Helper class to work with the a hub connection
    /// </summary>
    public static class HubConnectionHelpers
    {
        public static IHubConnectionBuilder FromUrl(string url)
        {
            Requires.NotNullOrEmpty(url, nameof(url));
            return new HubConnectionBuilder().WithUrl(url)
#if SignalR_Client_Version3
                .AddNewtonsoftJsonProtocol();
#else
                ;
#endif
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
            })
#if SignalR_Client_Version3
                .AddNewtonsoftJsonProtocol();
#else
                ;
#endif
        }

        public static async Task ConnectAsync(
            this HubConnection hubConnection,
            Func<int, int, Exception, Task<int>> onConnectCallback,
            int maxRetries,
            int delayMilliseconds,
            int maxDelayMilliseconds,
            TraceSource traceSource,
            CancellationToken cancellationToken)
        {
            Requires.NotNull(hubConnection, nameof(hubConnection));
            Requires.NotNull(traceSource, nameof(traceSource));

            var exponentialBackoff = new ExponentialBackoff(maxRetries, delayMilliseconds, maxDelayMilliseconds);
            while (true)
            {
                try
                {
                    traceSource.Verbose($"hubConnection.StartAsync -> retries:{exponentialBackoff.Retries}");
                    await hubConnection.StartAsync(cancellationToken);
                    await onConnectCallback(exponentialBackoff.Retries, 0, null);
                    traceSource.Verbose($"Succesfully connected...");
                    break;
                }
                catch (Exception err)
                {
                    if (err is OperationCanceledException || err is TaskCanceledException)
                    {
                        throw;
                    }

                    int delay = exponentialBackoff.NextDelayMilliseconds();
                    traceSource.Error($"Failed to connect-> delay:{delay} err:{err.Message}");
                    delay = await onConnectCallback(exponentialBackoff.Retries, delay, err);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }
    }
}
