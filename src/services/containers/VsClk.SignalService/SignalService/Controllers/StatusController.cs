using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VsCloudKernel.SignalService.Controllers
{
    /// <summary>
    /// A controller to return overall status of the app
    /// </summary>
    public class StatusController : ControllerBase
    {
        private const string EndpointPrefix = "Endpoint=";

        private readonly AppSettings appSettings;
        private readonly PresenceService presenceService;
        private readonly HealthService healthService;
        private readonly IStartup startup;

        public StatusController(
            IOptions<AppSettings> appSettingsProvider,
            PresenceService presenceService,
            HealthService healthService,
            IStartup startup)
        {
            this.appSettings = appSettingsProvider.Value;
            this.presenceService = presenceService;
            this.healthService = healthService;
            this.startup = startup;
        }

        private ITokenValidationProvider TokenValidationProvider => this.startup.TokenValidationProvider;

        // GET: version
        [HttpGet]
        public object Get()
        {
            var securityKeys = TokenValidationProvider?.SecurityKeys.Select(k =>
            {
                if (k is X509SecurityKey securityKey)
                {
                    return new
                    {
                        securityKey.Certificate.Version,
                        IssuerName = securityKey.Certificate.IssuerName.Name
                    };
                }
                return null;
            }).ToArray();

            dynamic versionObj = new
            {
                this.presenceService.ServiceId,
                Name = "vsclk-core-signalservice",
                Health = new
                {
                    this.healthService.State,
                    Providers = GetProvidersStatus()
                },
                AppSettings = new
                {
                    this.appSettings.Stamp,
                    this.appSettings.BaseUri,
                    this.appSettings.ImageTag,
                    this.appSettings.AuthenticateMetadataServiceUri,
                    this.appSettings.UseTelemetryProvider
                },
                Startup.AzureSignalREnabled,
                this.startup.UseAzureSignalR,
                AzureSignalRConnections = GetAllAzureSignalRConnections(),
                TokenValidatorType = TokenValidationProvider?.GetType().Name,
                TokenValidationProvider?.Audience,
                TokenValidationProvider?.Issuer,
                BackplaneProviderTypes = this.presenceService.BackplaneProviders.Select(f => f.GetType().Name).ToArray(),
                SecurityKeys = securityKeys,
                ContactsStatistics = this.presenceService.GetContactStatistics(),
            };

            return versionObj;
        }

        private dynamic GetProvidersStatus()
        {
            var dynObject = new System.Dynamic.ExpandoObject();           
            foreach (var item in this.healthService.GetProvidersStatus())
            {
                dynObject.TryAdd(item.Item1.Name, item.Item2);
            }

            return dynObject;
        }

        private object[] GetAllAzureSignalRConnections()
        {
            return AzureSignalRHelpers.GetAllAzureSignalRConnections(this.startup.Configuration)
                .Select(pair => new
                {
                    Name = pair.Key,
                    Endpoint = GetUriFromAzureConnectionString(pair.Value)
                }).ToArray();
        }

        private static string GetUriFromAzureConnectionString(string azureConnectionString)
        {
            return azureConnectionString.Split(';')[0].Substring(EndpointPrefix.Length);
        }
    }
}
