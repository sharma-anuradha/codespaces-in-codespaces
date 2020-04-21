// <copyright file="EnvironmentSessionDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common.Models
{
    /// <summary>
    /// Port forwarding session parameters.
    /// </summary>
    public class EnvironmentSessionDetails : PartialEnvironmentSessionDetails
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentSessionDetails"/> class.
        /// </summary>
        /// <param name="workspaceId">The liveshare workspace id.</param>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="port">The shared port.</param>
        public EnvironmentSessionDetails(string workspaceId, string environmentId, int port)
            : base(environmentId, port)
        {
            WorkspaceId = !string.IsNullOrWhiteSpace(workspaceId) ? workspaceId : throw new ArgumentNullException(nameof(workspaceId));
        }

        /// <summary>
        /// Gets the liveshare workspace id.
        /// </summary>
        public string WorkspaceId { get; }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is EnvironmentSessionDetails parameters &&
                   base.Equals(obj) &&
                   WorkspaceId == parameters.WorkspaceId;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), WorkspaceId, Port, EnvironmentId);
        }

        /// <summary>
        /// Deconstructs current instance for easier use in pattern matching.
        /// </summary>
        /// <param name="workspaceId">The current workspace id.</param>
        /// <param name="environmentId">The current environment id.</param>
        /// <param name="port">The port.</param>
        public void Deconstruct(out string workspaceId, out string environmentId, out int port)
        {
            workspaceId = WorkspaceId;
            environmentId = EnvironmentId;
            port = Port;
        }
    }
}