// <copyright file="ResourceAllocationKeepAlive.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{

    /// <summary>
    /// Represents a backend resources keepalive info in the context of the frontent.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ResourceAllocationKeepAlive
    {
        /// <summary>
        /// Gets or sets the last time the resource was found.
        /// </summary>
        [JsonProperty]
        public DateTime? ResourceAlive { get; set; }
    }
}
