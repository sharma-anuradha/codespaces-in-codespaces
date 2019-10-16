// <copyright file="EnvironmentData.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Represents the state of a Linux container based cloud environment.
    /// </summary>
    [DataContract]
    public class EnvironmentData : CollectedData
    {
        /// <summary>
        /// Gets or Sets the environment state.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [DataMember]
        public VsoEnvironmentState State { get; set; }

        /// <summary>
        /// Gets or Sets the environment id.
        /// </summary>
        [DataMember]
        public string EnvironmentId { get; set; }

        /// <summary>
        /// Gets or Sets the Session Path.
        /// </summary>
        [DataMember]
        public string SessionPath { get; set; }

        /// <summary>
        /// Gets or sets the Environment Type.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [DataMember]
        public VsoEnvironmentType EnvironmentType { get; set; }
    }
}
