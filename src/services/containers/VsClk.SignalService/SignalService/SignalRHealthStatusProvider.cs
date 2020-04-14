// <copyright file="SignalRHealthStatusProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
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
        private const string MethodEchoHealthHubFailedScope = "EchoHealthHubFailed";
        private const string MethodEchoHealthHubReconnectScope = "EchoHealthHubReconnect";
        private const string MethodEchoHealthHubOutOfCapacityScope = "EchoHealthOutOfCapacity";

        /// <summary>
        /// Time in minutes to perform an echo when the state is 'healthy'
        /// </summary>
        private const int EchoMinutesSucceed = 5;

        /// <summary>
        /// Time in secs to perform the echo when the state is 'unhealthy'
        /// </summary>
        private const int EchoSecsFailure = 45;

        /// <summary>
        /// Initial delay in secs after we can start perform our helthy tests
        /// </summary>
        private const int InitialDelayMinutes = 2;

        private readonly WarmupService warmupService;
        private readonly IWebHostEnvironment hostingEnvironment;
        private readonly AppSettings appSettings;
        private readonly ILogger logger;
        private int errorCount;

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
            State = true;
        }

        public bool State { get; private set; }

        private string HealthHubUrl => $"{appSettings.BaseUri}{Startup.HealthHubMap}";

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await this.warmupService.CompletedAsync();
            await Task.Delay(TimeSpan.FromMinutes(InitialDelayMinutes), cancellationToken);

            this.logger.LogDebug($"Starting health hub probe");

            while (true)
            {
                try
                {
                    if (!string.IsNullOrEmpty(appSettings.BaseUri))
                    {
                        var result = await EchoHealthHubAsync(cancellationToken);
                        if (State == false)
                        {
                            using (logger.BeginMethodScope(MethodEchoHealthHubReconnectScope))
                            {
                                this.logger.LogInformation($"Succesfully reconnect to signalr resources, errorCount:{this.errorCount}");
                            }
                        }

                        this.errorCount = 0;
                        this.logger.LogDebug($"Succesfully received echo -> message:{result.Message}");
                        State = true;
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
                        State = false;
                        using (logger.BeginMethodScope(MethodEchoHealthHubFailedScope))
                        {
                            ++this.errorCount;
                            const string HealthFailedConnectMessage = "Failed to connect to health hub with url:{0} errorCount:{1}";

                            if (this.errorCount == 1)
                            {
                                this.logger.LogWarning(error, HealthFailedConnectMessage, HealthHubUrl, 1);
                            }
                            else
                            {
                                this.logger.LogError(error, HealthFailedConnectMessage, HealthHubUrl, this.errorCount);
                            }
                        }
                    }
                }

                // delay depending on the State
                await Task.Delay(State ? TimeSpan.FromMinutes(EchoMinutesSucceed) : TimeSpan.FromSeconds(EchoSecsFailure), cancellationToken);
            }
        }

        private async Task<EchoResult> EchoHealthHubAsync(CancellationToken cancellationToken)
        {
            var hubConnection = new HubConnectionBuilder().WithUrl(HealthHubUrl).Build();
            await hubConnection.StartAsync(cancellationToken);
            var result = await hubConnection.InvokeAsync<EchoResult>(nameof(HealthServiceHub.Echo), EchoMessage, cancellationToken);
            await hubConnection.StopAsync(cancellationToken);
            return result;
        }
    }
}
