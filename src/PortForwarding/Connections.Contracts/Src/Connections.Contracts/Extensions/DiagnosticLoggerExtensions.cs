// <copyright file="DiagnosticLoggerExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IDiagnosticsLogger"/>.
    /// </summary>
    public static class DiagnosticLoggerExtensions
    {
        /// <summary>
        /// Adds agent registration values to the logger.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="agentRegistration">The agent registration.</param>
        /// <returns>The logger with additional values.</returns>
        public static IDiagnosticsLogger AddAgentRegistration(this IDiagnosticsLogger logger, AgentRegistration agentRegistration)
        {
            return logger
                .FluentAddValue("ConnectionId", agentRegistration.Name)
                .FluentAddValue("EnvironmentId", agentRegistration.Uid);
        }

        /// <summary>
        /// Adds connection details values to the logger.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="connectionDetails">The connection details.</param>
        /// <returns>The logger with additional values.</returns>
        public static IDiagnosticsLogger AddConnectionDetails(this IDiagnosticsLogger logger, ConnectionDetails connectionDetails)
        {
            return logger
                .FluentAddValue("ConnectionId", connectionDetails.WorkspaceId)
                .FluentAddValue("EnvironmentId", connectionDetails.EnvironmentId)
                .FluentAddValue("SourcePort", connectionDetails.SourcePort)
                .FluentAddValue("DestinationPort", connectionDetails.DestinationPort)
                .FluentAddValue("AgentName", connectionDetails.AgentName)
                .FluentAddValue("AgentUid", connectionDetails.AgentUid);
        }

        /// <summary>
        /// Adds connection request values to the logger.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="connectionRequest">The connection request.</param>
        /// <returns>The logger with additional values.</returns>
        public static IDiagnosticsLogger AddConnectionDetails(this IDiagnosticsLogger logger, ConnectionRequest connectionRequest)
        {
            return logger
                .FluentAddValue("ConnectionId", connectionRequest.WorkspaceId)
                .FluentAddValue("EnvironmentId", connectionRequest.EnvironmentId)
                .FluentAddValue("Port", connectionRequest.Port)
                .FluentAddValue("VSLiveShareApiEndpoint", connectionRequest.VSLiveShareApiEndpoint);
        }
    }
}
