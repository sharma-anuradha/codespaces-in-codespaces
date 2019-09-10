using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Common.Warmup;

namespace Microsoft.VsCloudKernel.SignalService
{
    public class AzureDocumentsProviderService : WarmupServiceBase
    {
        private readonly IOptions<AppSettings> appSettingsProvider;
        private readonly PresenceService presenceService;
        private readonly IStartup startup;
        private readonly ILogger<AzureDocumentsProvider> logger;
        private readonly IHubFormatProvider formatProvider;

        public AzureDocumentsProviderService(
            IList<IAsyncWarmup> warmupServices,
            IList<IHealthStatusProvider> healthStatusProviders,
            IOptions<AppSettings> appSettingsProvider,
            PresenceService service,
            IStartup startup,
            ILogger<AzureDocumentsProvider> logger,
            IHubFormatProvider formatProvider = null)
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
            if (AppSettings.IsAzureDocumentsProviderEnabled &&
                !string.IsNullOrEmpty(AppSettings.AzureCosmosDbEndpointUrl) &&
                !string.IsNullOrEmpty(AppSettings.AzureCosmosDbAuthKey))
            {
                var endpointUrl = NormalizeSetting(AppSettings.AzureCosmosDbEndpointUrl);
                var authorizationKey = NormalizeSetting(AppSettings.AzureCosmosDbAuthKey);

                this.logger.LogInformation($"Creating Azure Cosmos provider with Url:'{endpointUrl}'");
                try
                {
                    var backplaneProvider = await AzureDocumentsProvider.CreateAsync(
                        this.presenceService.ServiceId,
                        new DatabaseSettings()
                        {
                            EndpointUrl = endpointUrl,
                            AuthorizationKey = authorizationKey,
                            IsProduction = !this.startup.IsDevelopmentEnv
                        },
                        this.logger,
                        this.formatProvider);
                    this.presenceService.AddBackplaneProvider(backplaneProvider);
                }
                catch (Exception error)
                {
                    CompleteWarmup(false);
                    this.logger.LogError(error, $"Failed to create Azure Cosmos provider with Url:'{endpointUrl}'");
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
