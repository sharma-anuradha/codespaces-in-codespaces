// <copyright file="SeedInfoInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    /// <summary>
    /// The environment seed info.
    /// </summary>
    public class SeedInfoInput
    {
        /// <summary>
        /// Gets or sets the seed type.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "type")]
        public string SeedType { get; set; }

        /// <summary>
        /// Gets or sets the seed moniker.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "moniker")]
        public string SeedMoniker { get; set; }

        /// <summary>
        /// Gets or sets the Git configuration.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "gitConfig")]
        public GitConfigInput GitConfig { get; set; }
    }
}
