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
        private readonly ILogger<DatabaseBackplaneProvider> logger;

        private const int TimespanUpdateServiceSecs = 45;

        public DatabaseBackplaneProviderService(
            IList<IAsyncWarmup> warmupServices,
            IList<IHealthStatusProvider> healthStatusProviders,
            IOptions<AppSettings> appSettingsProvider,
            PresenceService service,
            ILogger<DatabaseBackplaneProvider> logger)
            : base(warmupServices, healthStatusProviders)
        {
            this.appSettingsProvider = appSettingsProvider;
            this.presenceService = service;
            this.logger = logger;
        }

        private AppSettings AppSettings => this.appSettingsProvider.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            DatabaseBackplaneProvider databaseBackplaneProvider = null;
            Func<Task> updateServiceCallback = () => databaseBackplaneProvider.UpdateService(
                this.presenceService.ServiceId,
                this.appSettingsProvider.Value.Stamp,
                this.presenceService.GetContactStatistics().TotalSelfCount,
                stoppingToken);
            if (!string.IsNullOrEmpty(AppSettings.AzureCosmosDbEndpointUrl) && !string.IsNullOrEmpty(AppSettings.AzureCosmosDbAuthKey))
            {
                var endpointUrl = NormalizeSetting(AppSettings.AzureCosmosDbEndpointUrl);
                var authorizationKey = NormalizeSetting(AppSettings.AzureCosmosDbAuthKey);

                this.logger.LogInformation($"Creating DatabaseProviderFactory with Url:'{endpointUrl}'");
                try
                {
                    databaseBackplaneProvider = await DatabaseBackplaneProvider.CreateAsync(
                        new DatabaseSettings()
                        {
                            EndpointUrl = endpointUrl,
                            AuthorizationKey = authorizationKey
                        },
                        this.logger);
                    await updateServiceCallback();
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

            // we will periodically refresh the service to avoid stale entries on the db data
            while (true)
            {
                try
                {
                    await updateServiceCallback();
                }
                catch (Exception error)
                {
                    this.logger.LogError(error, $"Failed to update service on the db backplae provider");
                }

                // delay depending on the State
                await Task.Delay(TimeSpan.FromSeconds(TimespanUpdateServiceSecs), stoppingToken);
            }
        }

        private static string NormalizeSetting(string s)
        {
            return System.Text.RegularExpressions.Regex.Replace(s, @"\r\n?|\n", string.Empty);
        }
    }
}
