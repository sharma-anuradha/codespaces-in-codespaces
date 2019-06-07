using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.VsCloudKernel.SignalService
{
    public class SignalRHealthStatusProvider : BackgroundService, IHealthStatusProvider
    {
        private const string EchoMessage = "signalr";
        private const int EchoMinutes = 5;
        private const int InitialDelayMinutes = 2;

        private readonly WarmupService warmupService;
        private readonly IHostingEnvironment hostingEnvironment;
        private readonly AppSettings appSettings;
        private readonly ILogger logger;

        public SignalRHealthStatusProvider(
            WarmupService warmupService,
            IHostingEnvironment hostingEnvironment,
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

        private string HealthHubUrl => $"{appSettings.BaseUri}{Startup.HealthHubMap}";

        public bool State { get; private set; }

        #region BackgroundService Override

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
                        var result = await EchoHealthHub(cancellationToken);
                        if (State == false)
                        {
                            this.logger.LogInformation($"Succesfully reconnect to signalr resources");
                        }

                        this.logger.LogDebug($"Succesfully received echo -> message:{result.Message} stamp:{result.Stamp} serviceId:{result.ServiceId}");
                        State = true;
                    }
                }
                catch (Exception error)
                {
                    State = false;
                    this.logger.LogError(error, $"Failed to connect to health hub with url:{HealthHubUrl}");
                }
                // Every 5 min
                await Task.Delay(TimeSpan.FromMinutes(EchoMinutes), cancellationToken);
            }
        }

        #endregion

        private async Task<EchoResult> EchoHealthHub(CancellationToken cancellationToken)
        {
            var hubConnection = new HubConnectionBuilder().WithUrl(HealthHubUrl).Build();
            await hubConnection.StartAsync(cancellationToken);
            var result = await hubConnection.InvokeAsync<EchoResult>(nameof(HealthServiceHub.Echo), EchoMessage, cancellationToken);
            await hubConnection.StopAsync(cancellationToken);
            return result;
        }
    }
}
