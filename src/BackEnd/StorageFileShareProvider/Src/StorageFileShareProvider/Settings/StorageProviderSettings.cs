// <copyright file="StorageProviderSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Settings
{
    /// <summary>
    /// Settings for the storage provider to use at runtime.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class StorageProviderSettings
    {
        /// <summary>
        /// Gets or sets the batch pool ID to be used by the storage file share provider.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string WorkerBatchPoolId { get; set; }
    }
}
