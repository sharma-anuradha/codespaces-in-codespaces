// <copyright file="ControlPlaneStampSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// The standard azure resource settings.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ControlPlaneStampSettings
    {
        /// <summary>
        /// Gets or sets the stamp name, e.g., "usw2".
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string StampName { get; set; }

        /// <summary>
        /// Gets or sets the DNS host name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DnsHostName { get; set; }

        /// <summary>
        /// Gets or sets the service bus resouce group name.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string ServiceBusResourceGroupName { get; set; }

        /// <summary>
        /// Gets or sets the the service bus namespace name.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string ServiceBusNamespaceName { get; set; }

        /// <summary>
        /// Gets or sets the list of supported data-plane locations.
        /// </summary>
        [JsonProperty(Required = Required.Always, ItemConverterType = typeof(StringEnumConverter))]
        public List<AzureLocation> DataPlaneLocations { get; set; } = new List<AzureLocation>();
    }
}