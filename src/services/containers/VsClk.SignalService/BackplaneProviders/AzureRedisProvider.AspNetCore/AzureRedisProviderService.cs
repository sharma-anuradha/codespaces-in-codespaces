using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VsCloudKernel.SignalService.Common;
using Microsoft.VsSaaS.Common.Warmup;
using StackExchange.Redis;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Async warm bootstrap for the Azure Redis provider
    /// </summary>
    public class AzureRedisProviderService : WarmupServiceBase
    {
        // Logger method scopes
        private const string MethodConnectionFailed = "ConnectionFailed";
        private const string MethodConnectionRestored = "ConnectionRestored";

        private readonly IOptions<AppSettingsBase> appSettingsProvider;
        private readonly ApplicationServicePrincipal applicationServicePrincipal;
        private readonly ILogger<AzureRedisProvider> logger;
        private readonly IDataFormatProvider formatProvider;
        private readonly IContactBackplaneManager backplaneManager;
        private readonly IStartupBase startup;

        public AzureRedisProviderService(
            IList<IAsyncWarmup> warmupServices,
            IList<IHealthStatusProvider> healthStatusProviders,
            IOptions<AppSettingsBase> appSettingsProvider,
            ApplicationServicePrincipal applicationServicePrincipal,
            IContactBackplaneManager backplaneManager,
            IStartupBase startup,
            ILogger<AzureRedisProvider> logger,
            IDataFormatProvider formatProvider = null)
            : base(warmupServices, healthStatusProviders)
        {
            this.appSettingsProvider = appSettingsProvider;
            this.applicationServicePrincipal = applicationServicePrincipal;
            this.backplaneManager = backplaneManager;
            this.startup = startup;
            this.logger = logger;
            this.formatProvider = formatProvider;
        }

        private AppSettingsBase AppSettings => this.appSettingsProvider.Value;

        private (string ServiceId, string Stamp) ServiceInfo => (this.startup.ServiceId, this.startup.Stamp);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bool isAzureRedisConnectionDefined = false;
            if (AppSettings.IsAzureRedisProviderEnabled &&
                (!string.IsNullOrEmpty(AppSettings.AzureRedisConnection) || (isAzureRedisConnectionDefined = this.applicationServicePrincipal.IsAzureRedisConnectionDefined(AppSettings))))
            {
                try
                {
                    var redisConfiguration = isAzureRedisConnectionDefined ?
                        await this.applicationServicePrincipal.GetAzureRedisConnection(
                            this.logger,
                            AppSettings,
                            stoppingToken) : AppSettings.AzureRedisConnection;

                    var options = ConfigurationOptions.Parse(redisConfiguration);
                    // define redis options here
                    options.AbortOnConnectFail = false;

                    var connections = new List<ConnectionMultiplexer>();
                    for (int n = 0; n < AppSettings.AzureRedisConnectionPool; ++n)
                    {
                        connections.Add(await CreateConnectionMultiplexerAsync(options, n));
                    }

                    var backplaneProvider = await AzureRedisProvider.CreateAsync(
                        ServiceInfo,
                        connections.ToArray(),
                        this.logger,
                        this.formatProvider);
                    this.backplaneManager.RegisterProvider(
                        backplaneProvider,
                        // Note: the redis provider does not support an optimized 'GetContacts' capability
                        // so when used with another provider with better support it will be discarded
                        new ContactBackplaneProviderSupportLevel()
                        { 
                            GetContacts = ContactBackplaneProviderSupportLevel.MinimumSupportThreshold
                        });
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

        private async Task<ConnectionMultiplexer> CreateConnectionMultiplexerAsync(ConfigurationOptions configurationOptions, int connectionNumber)
        {
            this.logger.LogInformation($"Creating n:{connectionNumber} Azure Redis Provider with host:{configurationOptions.SslHost}");
            var connection = await ConnectionMultiplexer.ConnectAsync(configurationOptions);

            connection.ConnectionFailed += (s, e) =>
            {
                this.logger.LogMethodScope(LogLevel.Warning, $"Connection n:{connectionNumber} type:{e.ConnectionType} Failure type:{e.FailureType}", MethodConnectionFailed);
            };

            connection.ConnectionRestored += (s, e) =>
            {
                this.logger.LogMethodScope(LogLevel.Warning, $"Connection n:{connectionNumber} type:{e.ConnectionType} ", MethodConnectionRestored);
            };

            return connection;
        }
    }
}
