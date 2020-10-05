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
        /// Gets or sets a value indicating whether to queue resource allocation.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        [Default(false)]
        public bool QueueResourceAllocation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to start with a shallow clone during creation.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        [Default(false)]
        public bool ShallowClone { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to install the git credential helper at the
        /// local/repo scope instead of system scope.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        [Default(false)]
        public bool LocalCredentialHelper { get; set; }
    }
}
