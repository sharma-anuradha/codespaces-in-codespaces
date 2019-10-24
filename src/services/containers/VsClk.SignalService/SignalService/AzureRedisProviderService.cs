using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Redis.Fluent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Common.Warmup;
using StackExchange.Redis;

namespace Microsoft.VsCloudKernel.SignalService
{
    public class AzureRedisProviderService : WarmupServiceBase
    {
        private readonly IOptions<AppSettings> appSettingsProvider;
        private readonly ApplicationServicePrincipal applicationServicePrincipal;
        private readonly PresenceService presenceService;
        private readonly ILogger<AzureRedisProvider> logger;
        private readonly IHubFormatProvider formatProvider;

        public AzureRedisProviderService(
            IList<IAsyncWarmup> warmupServices,
            IList<IHealthStatusProvider> healthStatusProviders,
            IOptions<AppSettings> appSettingsProvider,
            ApplicationServicePrincipal applicationServicePrincipal,
            PresenceService service,
            ILogger<AzureRedisProvider> logger,
            IHubFormatProvider formatProvider = null)
            : base(warmupServices, healthStatusProviders)
        {
            this.appSettingsProvider = appSettingsProvider;
            this.applicationServicePrincipal = applicationServicePrincipal;
            this.presenceService = service;
            this.logger = logger;
            this.formatProvider = formatProvider;
        }

        private AppSettings AppSettings => this.appSettingsProvider.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bool isAzureRedisConnectionDefined = false;
            if (!string.IsNullOrEmpty(AppSettings.AzureRedisConnection) || (isAzureRedisConnectionDefined = this.applicationServicePrincipal.IsAzureRedisConnectionDefined(AppSettings)))
            {
                try
                {
                    var redisConfiguration = isAzureRedisConnectionDefined ?
                        await this.applicationServicePrincipal.GetAzureRedisConnection(
                            this.logger,
                            AppSettings,
                            stoppingToken) : AppSettings.AzureRedisConnection;

                    var options = ConfigurationOptions.Parse(redisConfiguration);

                    this.logger.LogInformation($"Creating Azure Redis Provider with host:{options.SslHost}");
                    var connection = await ConnectionMultiplexer.ConnectAsync(options);

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
                    this.logger.LogError(error, $"Failed to create Azure Redis Provider");
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
