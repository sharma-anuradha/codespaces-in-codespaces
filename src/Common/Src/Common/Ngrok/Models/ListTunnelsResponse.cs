// <copyright file="ListTunnelsResponse.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Ngrok.Models
{
    /// <summary>
    /// Ngrok List Tunnels Response.
    /// </summary>
    public class ListTunnelsResponse
    {
        /// <summary>
        /// Gets or sets the tunnels.
        /// </summary>
        [JsonProperty(PropertyName = "tunnels")]
        public IEnumerable<Tunnel> Tunnels { get; set; }

        /// <summary>
        /// Gets or sets the local URI.
        /// </summary>
        [JsonProperty(PropertyName = "uri")]
        public string Uri { get; set; }
    }
}
