// <copyright file="LinuxDockerState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Common
{
    /// <summary>
    /// Represents the state of a Linux container based cloud environment.
    /// </summary>
    public class LinuxDockerState : AbstractMonitorState
    {
        /// <summary>
        /// Gets or Sets the environment state.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public EnvironmentRunningState State { get; set; }

        /// <summary>
        /// Gets or Sets the environment id.
        /// </summary>
        public string EnvironmentId { get; set; }

        /// <summary>
        /// Gets or Sets the Session Path.
        /// </summary>
        public string SessionPath { get; set; }
    }
}
