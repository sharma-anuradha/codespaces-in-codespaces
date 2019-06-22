using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Background service attached to the presence service lifetime
    /// </summary>
    public class PresenceBackgroundService : BackgroundService
    {
        private readonly WarmupService warmupService;
        private readonly IOptions<AppSettings> appSettingsProvider;
        private readonly PresenceService presenceService;

        private const int TimespanUpdateServiceSecs = 45;

        public PresenceBackgroundService(
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

            // next block will update the backplane metrics
            while (true)
            {
                await this.presenceService.UpdateBackplaneMetrics(new
                {
                    stamp = AppSettings.Stamp
                }, stoppingToken);

                await Task.Delay(TimeSpan.FromSeconds(TimespanUpdateServiceSecs), stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            // shutdown of the presence service
            await this.presenceService.DisposeAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
