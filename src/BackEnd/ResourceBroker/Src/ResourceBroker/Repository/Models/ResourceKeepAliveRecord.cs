// <copyright file="ResourceKeepAliveRecord.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models
{
    /// <summary>
    /// Tracks keep alives for resource record.
    /// </summary>
    public class ResourceKeepAliveRecord
    {
        /// <summary>
        /// Gets or sets the time that we last where notified that the
        /// azure resource was live.
        /// </summary>
        [JsonProperty(PropertyName = "azureResourceAlive")]
        public DateTime? AzureResourceAlive { get; set; }

        /// <summary>
        /// Gets or sets the time that we last where notified that the
        /// environment was live.
        /// </summary>
        [JsonProperty(PropertyName = "environmentAlive")]
        public DateTime? EnvironmentAlive { get; set; }
    }
}
