// <copyright file="HeartBeatBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Common
{
    /// <summary>
    /// The REST API body of a HeartBeat message.
    /// </summary>
    public class HeartBeatBody
    {
        /// <summary>
        /// Gets or sets uTC timestamp of the heartbeat.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Gets or sets vSO Agent version.
        /// </summary>
        public string AgentVersion { get; set; }

        /// <summary>
        /// Gets or sets virtual Machine Resource Id.
        /// </summary>
        public string ResourceId { get; set; }

        /// <summary>
        /// Gets or sets a map of monitor states for each monitor.
        /// </summary>
        [JsonProperty(ItemConverterType = typeof(MonitorStateConverter))]
        public KeyValuePair<string, AbstractMonitorState>[] MonitorStates { get; set; }
    }
}
