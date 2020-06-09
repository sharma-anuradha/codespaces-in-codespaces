// <copyright file="StartTunnelRequest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Ngrok.Models
{
    /// <summary>
    /// Creates an Ngrok Start Tunnel Request.
    /// </summary>
    public class StartTunnelRequest
    {
        /// <summary>
        /// Gets or sets the tunnel protocol name.
        /// </summary>
        [JsonProperty(PropertyName = "proto")] // Required
        public string Protocol { get; set; } // TODO make into enum

        /// <summary>
        /// Gets or sets the address to forward traffic to.
        /// </summary>
        [JsonProperty(PropertyName = "addr")] // Required
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets the name of the tunnel.
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether you can inspect the tunnel.
        /// </summary>
        [JsonProperty(PropertyName = "inspect")]
        public bool Inspect { get; set; }

        /// <summary>
        /// Gets or sets the authentication user parameters.
        /// </summary>
        [JsonProperty(PropertyName = "auth")]
        public string Auth { get; set; }

        /// <summary>
        /// Gets or sets the host header.
        /// </summary>
        [JsonProperty(PropertyName = "host_header")]
        public string HostHeader { get; set; }

        /// <summary>
        /// Gets or sets enabling binding to TLS. Can be 'true', 'false', or 'both'.
        /// </summary>
        [JsonProperty(PropertyName = "bind_tls")]
        public string BindTLS { get; set; }

        /// <summary>
        /// Gets or sets the subdomain.
        /// </summary>
        [JsonProperty(PropertyName = "subdomain")]
        public string Subdomain { get; set; }
    }
}
