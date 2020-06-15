// <copyright file="ResourceHeartBeatRecord.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models
{
    /// <summary>
    /// Resource HeartBeat Record.
    /// </summary>
    public class ResourceHeartBeatRecord
    {
        /// <summary>
        /// Gets or sets UTC timestamp of the heartbeat.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "timestamp")]
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Gets or sets vSO Agent version.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "agentVersion")]
        public string AgentVersion { get; set; }

        /// <summary>
        /// Gets or sets a list of data collected and sent by the VSOAgent.
        /// </summary>
        [JsonProperty(ItemConverterType = typeof(CollectedDataConverter), PropertyName = "collectedDataList")]
        public IEnumerable<CollectedData> CollectedDataList { get; set; }
    }
}
