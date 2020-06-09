// <copyright file="TunnelHttpMetrics.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Ngrok.Models
{
    /// <summary>
    /// Ngrok Tunnel Http Metrics.
    /// </summary>
    public class TunnelHttpMetrics
    {
        /// <summary>
        /// Gets or sets the count.
        /// </summary>
        [JsonProperty(PropertyName = "count")]
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets the rate1.
        /// </summary>
        [JsonProperty(PropertyName = "rate1")]
        public decimal Rate1 { get; set; }

        /// <summary>
        /// Gets or sets the rate5.
        /// </summary>
        [JsonProperty(PropertyName = "rate5")]
        public decimal Rate5 { get; set; }

        /// <summary>
        /// Gets or sets the rate15.
        /// </summary>
        [JsonProperty(PropertyName = "rate15")]
        public decimal Rate15 { get; set; }

        /// <summary>
        /// Gets or sets the p50.
        /// </summary>
        [JsonProperty(PropertyName = "p50")]
        public decimal P50 { get; set; }

        /// <summary>
        /// Gets or sets the p90.
        /// </summary>
        [JsonProperty(PropertyName = "p90")]
        public decimal P90 { get; set; }

        /// <summary>
        /// Gets or sets the p95.
        /// </summary>
        [JsonProperty(PropertyName = "p95")]
        public decimal P95 { get; set; }

        /// <summary>
        /// Gets or sets the p99.
        /// </summary>
        [JsonProperty(PropertyName = "p99")]
        public decimal P99 { get; set; }
    }
}
