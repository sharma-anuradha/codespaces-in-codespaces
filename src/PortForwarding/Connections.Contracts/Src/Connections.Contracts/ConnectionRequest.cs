// <copyright file="ConnectionRequest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts
{
    /// <summary>
    /// Connection details passed to port forwarding agent - used to establish LS connection.
    /// </summary>
    public class ConnectionRequest
    {
        /// <summary>
        /// Gets or sets LS Workspace ID.
        /// </summary>
        public string WorkspaceId { get; set; }

        /// <summary>
        /// Gets or sets Source port.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets Cascade / Access token accepted by LS agent.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the LiveShare API endpoint to be used.
        /// </summary>
        public string VSLiveShareApiEndpoint { get; set; }
    }
}
