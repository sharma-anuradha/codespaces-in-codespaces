// <copyright file="WorkspaceSessionDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common.Models
{
    /// <summary>
    /// Port forwarding session details.
    /// </summary>
    public class WorkspaceSessionDetails : PortForwardingSessionDetails
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkspaceSessionDetails"/> class.
        /// </summary>
        /// <param name="workspaceId">The liveshare workspaceId.</param>
        /// <param name="port">The shared port.</param>
        public WorkspaceSessionDetails(string workspaceId, int port)
            : base(port)
        {
            WorkspaceId = !string.IsNullOrWhiteSpace(workspaceId) ? workspaceId : throw new ArgumentNullException(nameof(workspaceId));
        }

        /// <summary>
        /// Gets the workspace id.
        /// </summary>
        public string WorkspaceId { get; }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is WorkspaceSessionDetails parameters &&
                   base.Equals(obj) &&
                   WorkspaceId == parameters.WorkspaceId;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(WorkspaceId, Port);
        }

        /// <summary>
        /// Deconstructs current instance for easier use in pattern matching.
        /// </summary>
        /// <param name="workspaceId">The current workspace id.</param>
        /// <param name="port">The port.</param>
        public void Deconstruct(out string workspaceId, out int port)
        {
            workspaceId = WorkspaceId;
            port = Port;
        }
    }
}