// <copyright file="StatusController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.SignalR;
using Microsoft.Extensions.Configuration;
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
        private readonly ContactService presenceService;
        private readonly RelayService relayService;
        private readonly HealthService healthService;
        private readonly Startup startup;
        private readonly IList<ServiceEndpoint> serviceEndpoints;

        public StatusController(
            IOptions<AppSettings> appSettingsProvider,
            ContactService presenceService,
            RelayService relayService,
            HealthService healthService,
            Startup startup,
            IList<ServiceEndpoint> serviceEndpoints)
        {
            this.appSettings = appSettingsProvider.Value;
            this.presenceService = presenceService;
            this.relayService = relayService;
            this.healthService = healthService;
            this.startup = startup;
            this.serviceEndpoints = serviceEndpoints;
        }

        private IConfigurationRoot Configuration => this.startup.Configuration;

        private ApplicationServicePrincipal ApplicationServicePrincipal => Configuration.GetSection(nameof(ApplicationServicePrincipal)).Get<ApplicationServicePrincipal>();

        // GET: version
        [HttpGet]
        public object Get()
        {
            dynamic versionObj = new
            {
                this.presenceService.ServiceId,
                Name = "vsclk-core-signalservice",
                NumberOfProcessors = System.Environment.ProcessorCount,
                this.startup.Environment,
                ApplicationClientId = ApplicationServicePrincipal?.ClientId,
                Health = new
                {
                    this.healthService.State,
                    Providers = GetProvidersStatus(),
                },
                AppSettings = new
                {
                    this.appSettings.Stamp,
                    this.appSettings.BaseUri,
                    this.appSettings.ImageTag,
                    this.appSettings.AuthenticateProfileServiceUri,
                    this.appSettings.UseTelemetryProvider,
                    this.appSettings.IsPrivacyEnabled,
                    this.appSettings.SubscriptionId,
                    this.appSettings.ResourceGroupName,
                    this.appSettings.ResourceGroupInstanceName,
                    this.appSettings.CorsOrigin,
                },
                this.startup.EnableAuthentication,
                this.startup.UseAzureSignalR,
                AzureSignalRConnections = GetAllAzureSignalRConnections(),
                BackplaneProviderTypes = this.presenceService.BackplaneManager.BackplaneProviders.Select(f => f.GetType().Name).ToArray(),
                PresenceMetrics = this.presenceService.GetMetrics(),
                RelayMetrics = this.relayService.GetMetrics(),
            };

            return versionObj;
        }

        private static string GetUriFromAzureConnectionString(string azureConnectionString)
        {
            return azureConnectionString.Split(';')[0].Substring(EndpointPrefix.Length);
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
            return this.serviceEndpoints.Select(se => new
            {
                Name = se.Name,
                Endpoint = se.Endpoint,
                Type = se.EndpointType.ToString(),
            }).ToArray();
        }
    }
}
