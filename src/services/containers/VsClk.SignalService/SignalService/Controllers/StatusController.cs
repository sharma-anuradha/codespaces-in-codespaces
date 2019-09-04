using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
        private readonly RelayService relayService;
        private readonly HealthService healthService;
        private readonly IStartup startup;

        public StatusController(
            IOptions<AppSettings> appSettingsProvider,
            PresenceService presenceService,
            RelayService relayService,
            HealthService healthService,
            IStartup startup)
        {
            this.appSettings = appSettingsProvider.Value;
            this.presenceService = presenceService;
            this.relayService = relayService;
            this.healthService = healthService;
            this.startup = startup;
        }

        // GET: version
        [HttpGet]
        public object Get()
        {
            dynamic versionObj = new
            {
                this.presenceService.ServiceId,
                Name = "vsclk-core-signalservice",
                this.startup.Environment,
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
                    this.appSettings.AuthenticateProfileServiceUri,
                    this.appSettings.UseTelemetryProvider,
                    this.appSettings.IsPrivacyEnabled
                },
                this.startup.EnableAuthentication,
                Startup.AzureSignalREnabled,
                this.startup.UseAzureSignalR,
                AzureSignalRConnections = GetAllAzureSignalRConnections(),
                BackplaneProviderTypes = this.presenceService.BackplaneProviders.Select(f => f.GetType().Name).ToArray(),
                PresenceMetrics = this.presenceService.GetMetrics(),
                RelayMetrics = this.relayService.GetMetrics(),
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
