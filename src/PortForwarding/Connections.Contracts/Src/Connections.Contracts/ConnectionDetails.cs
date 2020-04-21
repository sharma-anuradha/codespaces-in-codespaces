﻿// <copyright file="ConnectionDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts
{
    /// <summary>
    /// Connection details passed to port forwarding agent - used to establish LS connection.
    /// </summary>
    public class ConnectionDetails
    {
        /// <summary>
        /// Gets or sets LS Workspace ID.
        /// </summary>
        public string WorkspaceId { get; set; }

        /// <summary>
        /// Gets or sets the environment id for the connection.
        /// </summary>
        public string EnvironmentId { get; set; }

        /// <summary>
        /// Gets or sets source port.
        /// </summary>
        public int SourcePort { get; set; }

        /// <summary>
        /// Gets or sets destination port.
        /// </summary>
        public int DestinationPort { get; set; }

        /// <summary>
        /// Gets or sets agent name.
        /// </summary>
        public string AgentName { get; set; }

        /// <summary>
        /// Gets or sets agent Uid.
        /// </summary>
        public string AgentUid { get; set; }
    }
}
