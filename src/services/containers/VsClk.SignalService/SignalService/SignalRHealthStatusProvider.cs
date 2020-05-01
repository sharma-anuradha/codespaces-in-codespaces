// <copyright file="SignalRHealthStatusProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.SignalService
{
    public class SignalRHealthStatusProvider : BackgroundService, IHealthStatusProvider
    {
        private const string EchoMessage = "signalr";
        private const string MethodEchoHealthHubSucceedScope = "EchoHealthHubSucceed";
        private const string MethodEchoHealthHubFailedScope = "EchoHealthHubFailed";
        private const string MethodEchoHealthHubReconnectScope = "EchoHealthHubReconnect";
        private const string MethodEchoHealthHubOutOfCapacityScope = "EchoHealthOutOfCapacity";

        /// <summary>
        /// Time in minutes to perform an echo when the state is 'healthy'.
        /// </summary>
        private const int EchoMinutesSucceed = 5;

        /// <summary>
        /// Time in secs to perform the echo when the state is 'unhealthy'.
        /// </summary>
        private const int EchoSecsFailure = 45;

        /// <summary>
        /// Initial delay in secs after we can start perform our helthy tests.
        /// </summary>
        private const int InitialDelayMinutes = 2;

        private readonly WarmupService warmupService;
        private readonly IWebHostEnvironment hostingEnvironment;
        private readonly AppSettings appSettings;
        private readonly ILogger logger;
        private int errorCount;
        private int results;

        public SignalRHealthStatusProvider(
            WarmupService warmupService,
            IWebHostEnvironment hostingEnvironment,
            IList<IHealthStatusProvider> healthStatusProviders,
            IOptions<AppSettings> appSettingsProvider,
            ILogger<SignalRHealthStatusProvider> logger)
        {
            healthStatusProviders.Add(this);

            this.warmupService = warmupService;
            this.hostingEnvironment = hostingEnvironment;
            this.appSettings = appSettingsProvider.Value;
            this.logger = logger;
            IsHealthy = true;
        }

        public bool IsHealthy { get; private set; }

        public object Status => new
        {
            // if is Healthy
            this.IsHealthy,

            // report the actual error count of consecutiove failures
            ErrorCount = this.errorCount,

            // report the last states of the probe in binary format for example
            // 111001 => succeeded in the last attempt but 2 failures before that.
            SuccessResults = Convert.ToString(this.results, 2),
        };

        private string HealthHubUrl => $"{appSettings.BaseUri}{Startup.HealthHubMap}";

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await this.warmupService.CompletedAsync();
            await Task.Delay(TimeSpan.FromMinutes(InitialDelayMinutes), cancellationToken);

            this.logger.LogDebug($"Starting health hub probe");

            while (true)
            {
                var start = Stopwatch.StartNew();
                try
                {
                    if (!string.IsNullOrEmpty(appSettings.BaseUri))
                    {
                        // shift left results
                        results = results << 1;

                        var result = await EchoHealthHubAsync(cancellationToken);
                        if (IsHealthy == false)
                        {
                            using (logger.BeginMethodScope(MethodEchoHealthHubReconnectScope))
                            {
                                this.logger.LogInformation($"Succesfully reconnect to signalr resources, errorCount:{this.errorCount}");
                            }
                        }

                        this.errorCount = 0;
                        using (logger.BeginMethodScope(MethodEchoHealthHubSucceedScope))
                        {
                            this.logger.LogInformation($"Succesfully received echo -> time(ms):{start.ElapsedMilliseconds} stamp:{result.Stamp} serviceId:{result.ServiceId} message:{result.Message}");
                        }

                        IsHealthy = true;
                        ++results;
                    }
                }
                catch (Exception error)
                {
                    // Note: HTTP 429 will mean the SignalR resource is out of unit capacity, we don't want to report non-healthy
                    // in that scenario
                    if (error.Message?.Contains("429") == true)
                    {
                        using (logger.BeginMethodScope(MethodEchoHealthHubOutOfCapacityScope))
                        {
                            this.logger.LogError(error, $"Azure SignalR out of capacity on url:{HealthHubUrl}");
                        }
                    }
                    else
                    {
                        IsHealthy = false;
                        using (logger.BeginMethodScope(MethodEchoHealthHubFailedScope))
                        {
                            ++this.errorCount;
                            const string HealthFailedConnectMessage = "Failed to connect to health hub with url:{0} errorCount:{1} time(ms):{2}";

                            if (this.errorCount == 1)
                            {
                                this.logger.LogWarning(error, HealthFailedConnectMessage, HealthHubUrl, 1, start.ElapsedMilliseconds);
                            }
                            else
                            {
                                this.logger.LogError(error, HealthFailedConnectMessage, HealthHubUrl, this.errorCount, start.ElapsedMilliseconds);
                            }
                        }
                    }
                }

                // delay depending on the State
                await Task.Delay(IsHealthy ? TimeSpan.FromMinutes(EchoMinutesSucceed) : TimeSpan.FromSeconds(EchoSecsFailure), cancellationToken);
            }
        }

        private async Task<EchoResult> EchoHealthHubAsync(CancellationToken cancellationToken)
        {
            var hubConnection = new HubConnectionBuilder().WithUrl(HealthHubUrl, HttpTransportType.WebSockets).Build();
            hubConnection.HandshakeTimeout = TimeSpan.FromSeconds(45);

            await hubConnection.StartAsync(cancellationToken);
            var result = await hubConnection.InvokeAsync<EchoResult>(nameof(HealthServiceHub.Echo), EchoMessage, cancellationToken);
            await hubConnection.StopAsync(cancellationToken);
            return result;
        }
    }
}
