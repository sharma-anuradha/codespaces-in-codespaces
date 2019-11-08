using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Common.Warmup;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Async warm bootstrap for the Azure Cosmos provider
    /// </summary>
    public class AzureDocumentsProviderService : WarmupServiceBase
    {
        private readonly IOptions<AppSettingsBase> appSettingsProvider;
        private readonly ApplicationServicePrincipal applicationServicePrincipal;
        private readonly IContactBackplaneManager backplaneManager;
        private readonly IStartupBase startup;
        private readonly ILogger<AzureDocumentsProvider> logger;
        private readonly IDataFormatProvider formatProvider;

        public AzureDocumentsProviderService(
            IList<IAsyncWarmup> warmupServices,
            IList<IHealthStatusProvider> healthStatusProviders,
            IOptions<AppSettingsBase> appSettingsProvider,
            ApplicationServicePrincipal applicationServicePrincipal,
            IContactBackplaneManager backplaneManager,
            IStartupBase startup,
            ILogger<AzureDocumentsProvider> logger,
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
            bool isAzureCosmosConnectionDefined = false;

            if (AppSettings.IsAzureDocumentsProviderEnabled &&
                ((isAzureCosmosConnectionDefined = this.applicationServicePrincipal.IsAzureCosmosConnectionDefined(AppSettings)) ||
                (!string.IsNullOrEmpty(AppSettings.AzureCosmosDbEndpointUrl) &&
                !string.IsNullOrEmpty(AppSettings.AzureCosmosDbAuthKey))))
            {
                try
                {
                    var cosmosConfiguration = isAzureCosmosConnectionDefined ?
                        await this.applicationServicePrincipal.GetAzureCosmosConnection(
                            this.logger,
                            AppSettings,
                            stoppingToken) : (NormalizeSetting(AppSettings.AzureCosmosDbEndpointUrl), NormalizeSetting(AppSettings.AzureCosmosDbAuthKey));

                    this.logger.LogInformation($"Creating Azure Cosmos provider with Url:'{cosmosConfiguration.Item1}'");

                    var backplaneProvider = await AzureDocumentsProvider.CreateAsync(
                        ServiceInfo,
                        new DatabaseSettings()
                        {
                            EndpointUrl = cosmosConfiguration.Item1,
                            AuthorizationKey = cosmosConfiguration.Item2,
                            IsProduction = !this.startup.IsDevelopmentEnv
                        },
                        this.logger,
                        this.formatProvider);
                    this.backplaneManager.RegisterProvider(backplaneProvider);
                }
                catch (Exception error)
                {
                    CompleteWarmup(false);
                    this.logger.LogError(error, $"Failed to create Azure Cosmos provider");
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
