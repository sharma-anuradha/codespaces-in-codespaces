// <copyright file="TunnelMetrics.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Ngrok.Models
{
    /// <summary>
    /// Ngrok Tunnel Metrics.
    /// </summary>
    public class TunnelMetrics
    {
        /// <summary>
        /// Gets or sets the connection metrics.
        /// </summary>
        [JsonProperty(PropertyName = "conns")]
        public TunnelConnectionMetrics ConnectionMetrics { get; set; }

        /// <summary>
        /// Gets or sets the HTTP metrics.
        /// </summary>
        [JsonProperty(PropertyName = "http")]
        public TunnelHttpMetrics HttpMetrics { get; set; }
    }
}
