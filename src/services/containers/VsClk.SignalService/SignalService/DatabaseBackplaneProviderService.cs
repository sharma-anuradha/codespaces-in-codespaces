using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.VsCloudKernel.SignalService
{
    public class DatabaseBackplaneProviderService : BackgroundService
    {
        private readonly IOptions<AppSettings> appSettingsProvider;
        private readonly PresenceService service;
        private readonly ILogger<DatabaseBackplaneProvider> logger;

        public DatabaseBackplaneProviderService(
            IOptions<AppSettings> appSettingsProvider,
            PresenceService service,
            ILogger<DatabaseBackplaneProvider> logger)
        {
            this.appSettingsProvider = appSettingsProvider;
            this.service = service;
            this.logger = logger;
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
                        new DatabaseSettings()
                        {
                            EndpointUrl = endpointUrl,
                            AuthorizationKey = authorizationKey
                        },
                        this.logger);
                    this.service.AddBackplaneProvider(databaseBackplaneProvider);
                }
                catch (Exception error)
                {
                    this.logger.LogError($"Failed to create database with Url:'{endpointUrl}'. Error:{error}");
                }
            }
            else
            {
                this.logger.LogWarning($"Azure Cosmos not configured");
            }
        }

        private static string NormalizeSetting(string s)
        {
            return System.Text.RegularExpressions.Regex.Replace(s, @"\r\n?|\n", string.Empty);
        }
    }
}
