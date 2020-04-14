// <copyright file="HubConnectionHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// Helper class to work with the a hub connection.
    /// </summary>
    public static class HubConnectionHelpers
    {
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

        public static async Task ConnectAsync(
            this HubConnection hubConnection,
            Func<int, int, Exception, Task<int>> onConnectCallback,
            int maxRetries,
            int delayMilliseconds,
            int maxDelayMilliseconds,
            TraceSource traceSource,
            CancellationToken connectWaitingToken,
            CancellationToken cancellationToken)
        {
            Requires.NotNull(hubConnection, nameof(hubConnection));
            Requires.NotNull(traceSource, nameof(traceSource));

            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(connectWaitingToken, cancellationToken))
            {
                var exponentialBackoff = new ExponentialBackoff(maxRetries, delayMilliseconds, maxDelayMilliseconds);
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
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
                        int delay = exponentialBackoff.NextDelayMilliseconds();
                        traceSource.Error($"Failed to connect-> delay:{delay} name:{err.GetType().Name} err:{err.Message}");
                        delay = await onConnectCallback(exponentialBackoff.Retries, delay, err);
                        try
                        {
                            await Task.Delay(delay, linkedCts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                throw;
                            }
                        }
                    }
                }
            }
        }
    }
}
