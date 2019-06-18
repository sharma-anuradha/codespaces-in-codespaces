using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Update the metrics of the presence service
    /// </summary>
    public class UpdateMetricsService : BackgroundService
    {
        private readonly WarmupService warmupService;
        private readonly IOptions<AppSettings> appSettingsProvider;
        private readonly PresenceService presenceService;

        private const int TimespanUpdateServiceSecs = 45;

        public UpdateMetricsService(
            WarmupService warmupService,
            IOptions<AppSettings> appSettingsProvider,
            PresenceService service)
        {
            this.warmupService = warmupService;
            this.appSettingsProvider = appSettingsProvider;
            this.presenceService = service;
        }

        private AppSettings AppSettings => this.appSettingsProvider.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await this.warmupService.CompletedValueAsync();
            while (true)
            {
                await this.presenceService.UpdateBackplaneMetrics(new
                {
                    stamp = AppSettings.Stamp
                }, stoppingToken);

                await Task.Delay(TimeSpan.FromSeconds(TimespanUpdateServiceSecs), stoppingToken);
            }
        }
    }
}
