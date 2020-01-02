// <copyright file="ExperimentalFeaturesBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Bond;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Environments
{
    /// <summary>
    /// The experimental features body.
    /// </summary>
    public class ExperimentalFeaturesBody
    {
        /// <summary>
        /// Gets or sets a value indicating whether to use custom containers for this environment.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        [Default(false)]
        public bool CustomContainers { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use the new terminal output for this environment.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        [Default(false)]
        public bool NewTerminal { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use the multiple workspace feature for this environment.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        [Default(false)]
        public bool EnableMultipleWorkspaces { get; set; }
    }
}
