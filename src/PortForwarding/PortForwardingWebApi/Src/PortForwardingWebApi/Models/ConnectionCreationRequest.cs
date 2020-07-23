// <copyright file="ConnectionCreationRequest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Models
{
    /// <summary>
    /// Connection request body.
    /// </summary>
    public class ConnectionCreationRequest
    {
        /// <summary>
        /// Gets or sets the connection codespace id.
        /// </summary>
        public string Id { get; set; } = default!;

        /// <summary>
        /// Gets or sets the connection port.
        /// </summary>
        public int Port { get; set; }
    }
}
