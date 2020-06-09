// <copyright file="TunnelConfig.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Ngrok.Models
{
    /// <summary>
    /// Ngrok Tunnel Configuration.
    /// </summary>
    public class TunnelConfig
    {
        /// <summary>
        /// Gets or sets the Ngrok address.
        /// </summary>
        [JsonProperty(PropertyName = "addr")]
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether you can inspect the tunnel.
        /// </summary>
        [JsonProperty(PropertyName = "inspect")]
        public bool Inspect { get; set; }
    }
}
