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
        /// LS Workspace ID
        /// </summary>
        public string WorkspaceId { get; set; }

        /// <summary>
        /// Source port
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Cascade / Access token accepted by LS agent.
        /// </summary>
        public string Token { get; set; }
    }
}
