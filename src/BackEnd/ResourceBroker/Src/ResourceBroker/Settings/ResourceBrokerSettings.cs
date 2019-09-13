// <copyright file="ResourceBrokerSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings
{
    /// <summary>
    /// Settings for the resource broker to use at runtime.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ResourceBrokerSettings
    {
        /// <summary>
        /// Gets or sets the name of the blob container that the Resource Broker can use for distributed leases.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string LeaseContainerName { get; set; }

        /// <summary>
        /// Gets or sets the name of the blob container that the Resource Broker can use for file share template blobs.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        [Obsolete("This property will be moved to the sku catalog.", false)]
        public string FileShareTemplateContainerName { get; set; }
    }
}
