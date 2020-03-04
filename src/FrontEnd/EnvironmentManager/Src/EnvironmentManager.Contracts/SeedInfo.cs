// <copyright file="SeedInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEnd.Common;
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
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "type")]
        public SeedType SeedType { get; set; }

        /// <summary>
        /// Gets or sets the seed moniker.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "moniker")]
        public string SeedMoniker { get; set; }

        /// <summary>
        /// Gets or sets the seed checkout.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "commit")]
        public string SeedCommit { get; set; }

        /// <summary>
        /// Gets or sets the Git configuration options.
        /// </summary>
        [GDPR(Action = GDPRAction.Export)]
        [JsonProperty(Required = Required.Default, PropertyName = "gitConfig")]
        public GitConfigOptions GitConfig { get; set; }
    }
}
