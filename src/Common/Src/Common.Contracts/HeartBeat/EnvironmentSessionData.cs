// <copyright file="EnvironmentSessionData.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Runtime.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Represents the state of connected sessions.
    /// </summary>
    [DataContract]
    public class EnvironmentSessionData : CollectedData
    {
        /// <summary>
        /// Gets or sets the environment id.
        /// </summary>
        [DataMember]
        public string EnvironmentId { get; set; }

        /// <summary>
        /// Gets or sets the connected session count.
        /// </summary>
        [DataMember]
        public int ConnectedSessionCount { get; set; }
    }
}
