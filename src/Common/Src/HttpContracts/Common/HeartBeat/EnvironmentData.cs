// <copyright file="EnvironmentData.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Common
{
    /// <summary>
    /// Represents the state of a Linux container based cloud environment.
    /// </summary>
    public class EnvironmentData : CollectedData
    {
        /// <summary>
        /// Gets or Sets the environment state.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("state")]
        public VsoEnvironmentState State { get; set; }

        /// <summary>
        /// Gets or Sets the environment id.
        /// </summary>
        [JsonProperty("environmentId")]
        public string EnvironmentId { get; set; }

        /// <summary>
        /// Gets or Sets the Session Path.
        /// </summary>
        [JsonProperty("sessionPath")]
        public string SessionPath { get; set; }

        /// <summary>
        /// Gets or sets the Environment Type.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("environmentType")]
        public VsoEnvironmentType EnvironmentType { get; set; }
    }
}
