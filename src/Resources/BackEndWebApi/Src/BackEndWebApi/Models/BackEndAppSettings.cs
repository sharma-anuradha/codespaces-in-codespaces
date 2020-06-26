// <copyright file="BackEndAppSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApi.Models
{
    /// <summary>
    /// Settings that are passed in to the service as config at runtime.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class BackEndAppSettings
    {
        /// <summary>
        /// Gets or sets resource-broker specific settings.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public ResourceBrokerSettings ResourceBrokerSettings { get; set; } = new ResourceBrokerSettings();

        /// <summary>
        /// Gets or sets storage provider specific settings.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public StorageProviderSettings StorageProviderSettings { get; set; } = new StorageProviderSettings();

        /// <summary>
        /// Gets or sets the mocks settings.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public MocksSettings MocksSettings { get; set; } = new MocksSettings();
    }
}
