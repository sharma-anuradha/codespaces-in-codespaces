// <copyright file="NgrokHostedService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Ngrok;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Ngrok.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore.Services
{
    /// <summary>
    /// Ngrok Hosted Service.
    /// </summary>
    public class NgrokHostedService : IHostedService
    {
        private readonly NgrokOptions options;
        private readonly NgrokProcessManager processMgr;
        private readonly NgrokHttpClient client;
        private readonly IServer server;
        private readonly IHostApplicationLifetime applicationLifetime;
        private readonly ILogger<NgrokHostedService> logger;

        private readonly TaskCompletionSource<IReadOnlyCollection<Tunnel>> tunnelTaskSource;
        private readonly TaskCompletionSource<IReadOnlyCollection<string>> serverAddressesSource;
        private readonly TaskCompletionSource<bool> shutdownSource;

        private readonly CancellationTokenSource cancellationTokenSource;
        private IEnumerable<Tunnel> tunnels;
        private bool hasStarted;

        /// <summary>
        /// Initializes a new instance of the <see cref="NgrokHostedService"/> class.
        /// </summary>
        /// <param name="optionsMonitor">The options monitor.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="server">The server.</param>
        /// <param name="applicationLifetime">The application lifetime.</param>
        /// <param name="processMgr">The process manager.</param>
        /// <param name="client">The Ngork HTTP Client.</param>
        public NgrokHostedService(
            IOptionsMonitor<NgrokOptions> optionsMonitor,
            ILogger<NgrokHostedService> logger,
            IServer server,
            IHostApplicationLifetime applicationLifetime,
            NgrokProcessManager processMgr,
            NgrokHttpClient client)
        {
            this.logger = logger;
            options = optionsMonitor.CurrentValue;
            this.server = server;
            this.applicationLifetime = applicationLifetime;
            this.processMgr = processMgr;
            this.client = client;

            tunnelTaskSource = new TaskCompletionSource<IReadOnlyCollection<Tunnel>>();
            serverAddressesSource = new TaskCompletionSource<IReadOnlyCollection<string>>();
            shutdownSource = new TaskCompletionSource<bool>();
            cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Action when Ngrok is ready.
        /// </summary>
        public event Action<IReadOnlyCollection<Tunnel>> Ready;

        /// <summary>
        /// Gets Ngrok Tunnels.
        /// </summary>
        /// <returns>Collection of Tunnels.</returns>
        public async Task<IReadOnlyCollection<Tunnel>> GetTunnelsAsync()
        {
            if (options.Disable)
            {
                return Array.Empty<Tunnel>();
            }

            return await WaitForTaskWithTimeout(tunnelTaskSource.Task, (int)TimeSpan.FromMinutes(5).TotalMilliseconds, "No tunnels were found within 5 minutes. Perhaps the server was taking too long to start?");
        }

        /// <summary>
        /// Verifies if the ngrok process is running.
        /// </summary>
        /// <param name="timeoutTimeMinutes">Amount of time to check if Ngrok is running. Defaults to 5 minutes.</param>
        /// <returns>A boolean.</returns>
        public async Task<bool> IsNgrokRunning(int timeoutTimeMinutes = 5)
        {
            var cancellationToken = new CancellationTokenSource();
            cancellationToken.CancelAfter(TimeSpan.FromMinutes(timeoutTimeMinutes));
            var ngrokRunning = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay((int)TimeSpan.FromSeconds(5).TotalMilliseconds);
                ngrokRunning = await this.client.IsNgrokRunningAsync();
                if (ngrokRunning)
                {
                    cancellationToken.Cancel();
                }
            }

            return ngrokRunning;
        }

        /// <summary>
        /// Starts Ngrok Session.
        /// </summary>
        /// <param name="cancellationToken">Token for canceling the task.</param>
        /// <returns>A Task.</returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                cancellationToken.Register(() => cancellationTokenSource.Cancel());
                applicationLifetime.ApplicationStarted.Register(() => OnApplicationStarted());
            });
        }

        /// <summary>
        /// Stops Ngrok Session.
        /// </summary>
        /// <param name="cancellationToken">Token for canceling the task.</param>
        /// <returns>A Task.</returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationTokenSource.Cancel();

            await shutdownSource.Task;
        }

        /// <summary>
        /// Runs when Application Starts.
        /// </summary>
        /// <returns>Task.</returns>
        public Task OnApplicationStarted()
        {
            if (hasStarted)
            {
                return Task.CompletedTask;
            }

            hasStarted = true;
            var addresses = server.Features.Get<IServerAddressesFeature>().Addresses.ToArray();
            logger.LogDebug("Inferred hosting URLs as {ServerAddresses}.", addresses);
            serverAddressesSource.SetResult(addresses.ToArray());
            return RunAsync(cancellationTokenSource.Token);
        }

        private async Task RunAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (options.Disable)
                {
                    return;
                }

                await processMgr.EnsureNgrokStartedAsync(cancellationToken);

                if (cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                var url = await AdjustApplicationHttpUrlIfNeededAsync();
                logger.LogInformation("Picked hosting URL {Url}.", url);

                if (cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                var tunnels = await ListTunnelsAsync(cancellationToken);

                // If we don't have any existing, try starting a new one.
                if (!tunnels.Any())
                {
                    tunnels = await StartTunnelsAsync(url, cancellationToken);
                }

                logger.LogInformation("Tunnels {Tunnels} have been started.", new object[] { tunnels });

                if (cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                if (tunnels != null)
                {
                    OnTunnelsFetched(tunnels);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occured while running the Ngrok service.");
            }
            finally
            {
                shutdownSource.SetResult(true);
            }
        }

        private void OnTunnelsFetched(IEnumerable<Tunnel> tunnels)
        {
            if (tunnels == null)
            {
                throw new ArgumentNullException(nameof(tunnels), "Tunnels was not expected to be null here.");
            }

            this.tunnels = tunnels;
            tunnelTaskSource.SetResult(tunnels.ToArray());
            Ready?.Invoke(tunnels.ToArray());
        }

        private async Task<Tunnel[]> StartTunnelsAsync(string address, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(address))
            {
                address = "80";
            }
            else
            {
                if (!int.TryParse(address, out _))
                {
                    var url = new Uri(address);
                    if (url.Port != 80 && url.Port != 443)
                    {
                        address = $"{url.Host}:{url.Port}";
                    }
                    else
                    {
                        if (address.StartsWith("http://"))
                        {
                            address = address.Remove(address.IndexOf("http://"), "http://".Length);
                        }

                        if (address.StartsWith("https://"))
                        {
                            address = address.Remove(address.IndexOf("https://"), "https://".Length);
                        }
                    }
                }
            }

            // Start Tunnel
            var tunnel = await client.StartTunnelAsync(
                new StartTunnelRequest()
            {
                Name = System.AppDomain.CurrentDomain.FriendlyName,
                Address = address,
                Protocol = "http",
                HostHeader = address,
            }, cancellationToken);

            // Get Tunnels
            return await ListTunnelsAsync(cancellationToken);
        }

        private async Task<Tunnel[]> ListTunnelsAsync(CancellationToken cancellationToken)
        {
            // Get Tunnels
            return (await client.ListTunnelsAsync(cancellationToken))
                .Where(t => t.Name.Contains("(http)"))
                ?.ToArray() ?? new Tunnel[0];
        }

        private async Task<T> WaitForTaskWithTimeout<T>(Task<T> task, int timeoutInMilliseconds, string timeoutMessage)
        {
            if (await Task.WhenAny(task, Task.Delay(timeoutInMilliseconds, cancellationTokenSource.Token)) == task)
            {
                return await task;
            }

            throw new InvalidOperationException(timeoutMessage);
        }

        private async Task<string> AdjustApplicationHttpUrlIfNeededAsync()
        {
            var url = options.ApplicationHttpUrl;

            // TODO review if this is needed. Can this even be hit anymore?
            if (string.IsNullOrWhiteSpace(url))
            {
                var addresses = await WaitForTaskWithTimeout(
                    serverAddressesSource.Task,
                    30000,
                    $"No {nameof(NgrokOptions.ApplicationHttpUrl)} was set in the settings, and the URL of the server could not be inferred within 30 seconds.");
                if (addresses != null)
                {
                    url = addresses.FirstOrDefault(a => a.StartsWith("http://")) ?? addresses.FirstOrDefault();
                    url = url?.Replace("*", "localhost", StringComparison.InvariantCulture);
                }
            }

            options.ApplicationHttpUrl = url;

            if (url == null)
            {
                throw new InvalidOperationException("No application URL has been set, and it could not be inferred.");
            }

            return url;
        }
    }
}
