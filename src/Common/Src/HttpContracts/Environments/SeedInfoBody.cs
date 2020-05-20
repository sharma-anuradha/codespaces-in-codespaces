// <copyright file="SeedInfoBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments
{
    /// <summary>
    /// The environment seed info.
    /// </summary>
    public class SeedInfoBody
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
        public GitConfigOptionsBody GitConfig { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the submodules of the repository should be cloned.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "recurseClone")]
        public bool RecurseClone { get; set; }
    }
}
