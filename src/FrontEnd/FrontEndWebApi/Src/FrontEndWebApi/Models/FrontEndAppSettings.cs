// <copyright file="FrontEndAppSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
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
        /// Gets or sets the RPSaaS ApplicationID.
        /// This appid claim will be present for all api calls coming from RPSaaS.
        /// </summary>
        public string RPSaaSAppIdString { get; set; }

        /// <summary>
        /// Gets or sets the Authority URL used to valided RPSaaS
        /// signing signature.
        /// </summary>
        public string RPSaaSAuthorityString { get; set; }

        /// <summary>
        /// Gets or sets the Environment Manager Settings.
        /// </summary>
        public EnvironmentManagerSettings EnvironmentManagerSettings { get; set; }

        /// <summary>
        /// Gets or sets the SkuPlan Manager Settings.
        /// </summary>
        public PlanManagerSettings PlanManagerSettings { get; set; }
    }
}
