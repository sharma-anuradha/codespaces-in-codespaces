﻿// <copyright file="HeartBeatBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Common
{
    /// <summary>
    /// HeartBeat message from a VM.
    /// </summary>
    public class HeartBeatBody
    {
        /// <summary>
        /// Gets or sets UTC timestamp of the heartbeat.
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
        /// Gets or sets the Environment Id.
        /// </summary>
        [JsonProperty("environmentId")]
        public string EnvironmentId { get; set; }

        /// <summary>
        /// Gets or sets a list of data collected and sent by the VSOAgent.
        /// </summary>
        [JsonProperty(ItemConverterType = typeof(CollectedDataConverter), PropertyName = "collectedDataList")]
        public IEnumerable<CollectedData> CollectedDataList { get; set; }
    }
}
