// <copyright file="ResourceHeartBeatSummaryRecord.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models
{
    /// <summary>
    /// Resource HeartBeat Summary Record.
    /// </summary>
    public class ResourceHeartBeatSummaryRecord
    {
        /// <summary>
        /// Gets or Sets latest HeartBeat message with merged collected data.
        /// </summary>
        public ResourceHeartBeatRecord MergedHeartBeat { get; set; }

        /// <summary>
        /// Gets or Sets latest raw HeartBeat messages.
        /// </summary>
        public ResourceHeartBeatRecord LatestRawHeartBeat { get; set; }

        /// <summary>
        /// Gets or Sets the last heartbeat timestamp.
        /// </summary>
        public DateTime? LastSeen { get; set; }

        /// <summary>
        /// Gets a value indicating whether the VM is healthy. VM Is Unhealthy if it is not seen for more than 3 minutes.
        /// </summary>
        [JsonIgnore]
        public bool IsHealthy
        {
            get
            {
                if (LastSeen?.AddMinutes(3) < DateTime.UtcNow)
                {
                    return false;
                }

                return true;
            }
        }
    }
}
