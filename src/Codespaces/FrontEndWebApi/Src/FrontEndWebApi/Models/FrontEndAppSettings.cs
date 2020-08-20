// <copyright file="FrontEndAppSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.PCFAgent;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    /// <summary>
    /// Settings that are passed in to the service as config at runtime.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class FrontEndAppSettings
    {
        /// <summary>
        /// Gets or sets the authority to use for JWT token validation.
        /// </summary>
        public string AuthJwtAuthority { get; set; }

        /// <summary>
        /// Gets or sets the audiences to accept for JWT tokens.
        /// This should be a comma-delimited list of one or more audiences.
        /// </summary>
        public string AuthJwtAudiences { get; set; }

        /// <summary>
        /// Gets or sets the base address for the back-end web api.
        /// </summary>
        public string BackEndWebApiBaseAddress { get; set; }

        /// <summary>
        /// Gets or sets the Live Share API endpoint.
        /// </summary>
        public string VSLiveShareApiEndpoint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use mock providers for local development.
        /// </summary>
        public bool UseMocksForLocalDevelopment { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether non-critical background tasks are
        /// disabled for local development.
        /// </summary>
        public bool DisableBackgroundTasksForLocalDevelopment { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether environment can be exported
        /// disabled for local development.
        /// </summary>
        public bool EnableExporting { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use fake local docker deployment for cloud environment development.
        /// This would be useful mostly for CE CLI development and testing.
        /// </summary>
        public bool UseFakesForCECLIDevelopmentWithLocalDocker { get; set; }

        /// <summary>
        /// Gets or sets the local docker image name for development & testing.
        /// </summary>
        public string UseFakesLocalDockerImage { get; set; }

        /// <summary>
        /// Gets or sets the local published CLI path for development & testing.
        /// </summary>
        public string UseFakesPublishedCLIPath { get; set; }

        /// <summary>
        /// Gets or sets the forwarding url for local development & testing.
        /// </summary>
        public string ForwardingHostForLocalDevelopment { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to call the real backend during local development instead of mocks.
        /// </summary>
        public bool UseBackEndForLocalDevelopment { get; set; }

        /// <summary>
        /// Gets or sets the redis cache connection string.
        /// </summary>
        public string RedisConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the RPaaS settings.
        /// </summary>
        public RPaaSSettings RPaaSSettings { get; set; }

        /// <summary>
        /// Gets or sets the Environment Manager Settings.
        /// </summary>
        public EnvironmentManagerSettings EnvironmentManagerSettings { get; set; }

        /// <summary>
        /// Gets or sets the Environment Monitor Settings.
        /// </summary>
        public EnvironmentMonitorSettings EnvironmentMonitorSettings { get; set; }

        /// <summary>
        /// Gets or sets the SkuPlan Manager Settings.
        /// </summary>
        public PlanManagerSettings PlanManagerSettings { get; set; }

        /// <summary>
        /// Gets or sets privacy CommandFeed Settings.
        /// </summary>
        public PrivacyCommandFeedSettings PrivacyCommandFeedSettings { get; set; }

        /// <summary>
        /// Gets or sets the Subscription Manager Settings.
        /// </summary>
        public SubscriptionManagerSettings SubscriptionManagerSettings { get; set; }

        /// <summary>
        /// Gets or sets the Mdm metric settings.
        /// </summary>
        public MdmMetricSettings MdmMetricSettings { get; set; }

        /// <summary>
        /// Gets or sets the Billing Settings.
        /// </summary>
        public BillingSettings BillingSettings { get; set; }

        /// <summary>
        /// Gets or sets the Billing Meter settings.
        /// </summary>
        public BillingMeterSettings BillingMeterSettings { get; set; }

        /// <summary>
        /// Gets or sets the GitHub Proxy Settings by locations Dictionary.
        /// </summary>
        public Dictionary<string, GitHubProxySettings> GitHubProxySettingsByLocation { get; set; }
    }
}
