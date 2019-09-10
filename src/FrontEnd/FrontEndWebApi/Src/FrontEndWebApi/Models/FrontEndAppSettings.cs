// <copyright file="FrontEndAppSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

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
    }
}
