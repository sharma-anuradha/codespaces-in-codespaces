// <copyright file="Tunnel.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Ngrok.Models
{
    /// <summary>
    /// Ngrok Tunnel.
    /// </summary>
    public class Tunnel
    {
        /// <summary>
        /// Gets or sets the name of the tunnel.
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the local URI.
        /// </summary>
        [JsonProperty(PropertyName = "uri")]
        public string URI { get; set; }

        /// <summary>
        /// Gets or sets the public url.
        /// </summary>
        [JsonProperty(PropertyName = "public_url")]
        public string PublicURL { get; set; }

        /// <summary>
        /// Gets or sets the protocol.
        /// </summary>
        [JsonProperty(PropertyName = "proto")]
        public string Proto { get; set; }

        /// <summary>
        /// Gets or sets the connection configuration.
        /// </summary>
        [JsonProperty(PropertyName = "config")]
        public TunnelConfig Config { get; set; }

        /// <summary>
        /// Gets or sets the tunnel metrics.
        /// </summary>
        [JsonProperty(PropertyName = "metrics")]
        public TunnelMetrics Metrics { get; set; }
    }
}
