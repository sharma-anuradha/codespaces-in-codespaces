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

            await this.presenceService.RunAsync(new
            {
                stamp = AppSettings.Stamp
            }, stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            // shutdown of the presence service
            await this.presenceService.DisposeAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
