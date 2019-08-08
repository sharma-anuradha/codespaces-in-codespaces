// <copyright file="SeedInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// The environment seed info.
    /// </summary>
    public class SeedInfo
    {
        /// <summary>
        /// Gets or sets the seed type.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "type")]
        public SeedType SeedType { get; set; }

        /// <summary>
        /// Gets or sets the seed moniker.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "moniker")]
        public string SeedMoniker { get; set; }

        /// <summary>
        /// Gets or sets the Git configuration options.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "gitConfig")]
        public GitConfigOptions GitConfig { get; set; }
    }
}
