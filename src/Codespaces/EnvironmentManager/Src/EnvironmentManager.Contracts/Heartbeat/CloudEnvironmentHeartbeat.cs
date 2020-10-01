// <copyright file="CloudEnvironmentHeartbeat.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// The top-level environment heartbeat entity.
    /// </summary>
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class CloudEnvironmentHeartbeat : TaggedEntity
    {
        private ResourceAllocationKeepAlive keepAlive;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEnvironmentHeartbeat"/> class.
        /// </summary>
        public CloudEnvironmentHeartbeat()
        {
            Id = Guid.NewGuid().ToString();
            Created = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets or sets the last time the record is updated based on heartbeat.
        /// </summary>
        public DateTime? LastUpdatedByHeartBeat { get; set; }

        /// <summary>
        /// Gets or sets the last time the record is created.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the resources keep alive.
        /// </summary>
        public ResourceAllocationKeepAlive KeepAlive
        {
            get { return keepAlive ?? (keepAlive = new ResourceAllocationKeepAlive()); }
            set { keepAlive = value; }
        }
    }
}