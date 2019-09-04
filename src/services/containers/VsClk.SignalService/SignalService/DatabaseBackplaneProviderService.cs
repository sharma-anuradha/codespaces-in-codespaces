using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Common.Warmup;

namespace Microsoft.VsCloudKernel.SignalService
{
    public class DatabaseBackplaneProviderService : WarmupServiceBase
    {
        private readonly IOptions<AppSettings> appSettingsProvider;
        private readonly PresenceService presenceService;
        private readonly IStartup startup;
        private readonly ILogger<DatabaseBackplaneProvider> logger;
        private readonly IHubFormatProvider formatProvider;

        public DatabaseBackplaneProviderService(
            IList<IAsyncWarmup> warmupServices,
            IList<IHealthStatusProvider> healthStatusProviders,
            IOptions<AppSettings> appSettingsProvider,
            PresenceService service,
            IStartup startup,
            ILogger<DatabaseBackplaneProvider> logger,
            IHubFormatProvider formatProvider)
            : base(warmupServices, healthStatusProviders)
        {
            this.appSettingsProvider = appSettingsProvider;
            this.presenceService = service;
            this.startup = startup;
            this.logger = logger;
            this.formatProvider = formatProvider;
        }

        private AppSettings AppSettings => this.appSettingsProvider.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!string.IsNullOrEmpty(AppSettings.AzureCosmosDbEndpointUrl) && !string.IsNullOrEmpty(AppSettings.AzureCosmosDbAuthKey))
            {
                var endpointUrl = NormalizeSetting(AppSettings.AzureCosmosDbEndpointUrl);
                var authorizationKey = NormalizeSetting(AppSettings.AzureCosmosDbAuthKey);

                this.logger.LogInformation($"Creating DatabaseProviderFactory with Url:'{endpointUrl}'");
                try
                {
                    var databaseBackplaneProvider = await DatabaseBackplaneProvider.CreateAsync(
                        this.presenceService.ServiceId,
                        new DatabaseSettings()
                        {
                            EndpointUrl = endpointUrl,
                            AuthorizationKey = authorizationKey,
                            IsProduction = !this.startup.IsDevelopmentEnv
                        },
                        this.logger,
                        this.formatProvider);
                    this.presenceService.AddBackplaneProvider(databaseBackplaneProvider);
                }
                catch (Exception error)
                {
                    CompleteWarmup(false);
                    this.logger.LogError(error, $"Failed to create database with Url:'{endpointUrl}'");
                    throw;
                }
            }
            else
            {
                this.logger.LogWarning($"Azure Cosmos not configured");
            }

            CompleteWarmup(true);
        }

        private static string NormalizeSetting(string s)
        {
            return System.Text.RegularExpressions.Regex.Replace(s, @"\r\n?|\n", string.Empty);
        }
    }
}
