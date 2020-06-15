// <copyright file="MetricsInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Metrics;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// The environment connection info.
    /// </summary>
    public class MetricsInfo
    {
        /// <summary>
        /// Gets or sets the 2-letter ISO country code from the originating HTTP request.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "isoCountryCode")]
        public string IsoCountryCode { get; set; }

        /// <summary>
        /// Gets or sets the Azure geography corresponding to the <see cref="IsoCountryCode"/>.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "azureGeography")]
        public AzureGeography? AzureGeography { get; set; }

        /// <summary>
        /// Gets or sets the VSO client type from the originating HTTP requtest.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "vsoClientType")]
        public VsoClientType? VsoClientType { get; set; }
    }
}
