using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Common.Warmup;
using StackExchange.Redis;

namespace Microsoft.VsCloudKernel.SignalService
{
    public class AzureRedisProviderService : WarmupServiceBase
    {
        private readonly IOptions<AppSettings> appSettingsProvider;
        private readonly PresenceService presenceService;
        private readonly ILogger<AzureRedisProvider> logger;
        private readonly IHubFormatProvider formatProvider;

        public AzureRedisProviderService(
            IList<IAsyncWarmup> warmupServices,
            IList<IHealthStatusProvider> healthStatusProviders,
            IOptions<AppSettings> appSettingsProvider,
            PresenceService service,
            IStartup startup,
            ILogger<AzureRedisProvider> logger,
            IHubFormatProvider formatProvider = null)
            : base(warmupServices, healthStatusProviders)
        {
            this.appSettingsProvider = appSettingsProvider;
            this.presenceService = service;
            this.logger = logger;
            this.formatProvider = formatProvider;
        }

        private AppSettings AppSettings => this.appSettingsProvider.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!string.IsNullOrEmpty(AppSettings.AzureRedisConnection))
            {
                this.logger.LogInformation($"Creating Azure Redis Provider ...");
                try
                {
                    var connection = await ConnectionMultiplexer.ConnectAsync(AppSettings.AzureRedisConnection);

                    var backplaneProvider = await AzureRedisProvider.CreateAsync(
                        this.presenceService.ServiceId,
                        connection,
                        this.logger,
                        this.formatProvider);
                    this.presenceService.AddBackplaneProvider(backplaneProvider);
                }
                catch (Exception error)
                {
                    CompleteWarmup(false);
                    this.logger.LogError(error, $"Creating Azure Redis Provider");
                    throw;
                }
            }
            else
            {
                this.logger.LogWarning($"Azure Redis Provider not configured");
            }

            CompleteWarmup(true);
        }
    }
}
