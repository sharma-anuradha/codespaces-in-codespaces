// <copyright file="HeartBeatInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts
{
    /// <summary>
    /// HeartBeat message from a VM.
    /// </summary>
    public class HeartBeatInput
    {
        /// <summary>
        /// Gets or sets uTC timestamp of the heartbeat.
        /// </summary>
        [JsonProperty("timestamp")]
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Gets or sets vSO Agent version.
        /// </summary>
        [JsonProperty("agentVersion")]
        public string AgentVersion { get; set; }

        /// <summary>
        /// Gets or sets virtual Machine Resource Id.
        /// </summary>
        [JsonProperty("resourceId")]
        public Guid ResourceId { get; set; }

        /// <summary>
        /// Gets or sets a list of data collected and sent by the VSOAgent.
        /// </summary>
        [JsonProperty("collectedDataList")]
        public IEnumerable<CollectedData> CollectedDataList { get; set; }
    }
}
