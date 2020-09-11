// <copyright file="DiagnosticLoggerExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
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
            var scheme = connectionDetails.Hints?.UseHttps == true ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;

            return logger
                .FluentAddValue("ConnectionId", connectionDetails.WorkspaceId)
                .FluentAddValue("EnvironmentId", connectionDetails.EnvironmentId)
                .FluentAddValue("SourcePort", connectionDetails.SourcePort)
                .FluentAddValue("DestinationPort", connectionDetails.DestinationPort)
                .FluentAddValue("AgentName", connectionDetails.AgentName)
                .FluentAddValue("AgentUid", connectionDetails.AgentUid)
                .FluentAddValue("ServiceScheme", scheme);
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

        /// <summary>
        /// Adds connection request values to the logger.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="connectionRequest">The connection request.</param>
        /// <returns>The logger with additional values.</returns>
        public static IDiagnosticsLogger AddBaseConnectionDetails(this IDiagnosticsLogger logger, ConnectionRequest connectionRequest)
        {
            return logger
                .FluentAddBaseValue("ConnectionId", connectionRequest.WorkspaceId)
                .FluentAddBaseValue("EnvironmentId", connectionRequest.EnvironmentId)
                .FluentAddBaseValue("Port", connectionRequest.Port)
                .FluentAddBaseValue("VSLiveShareApiEndpoint", connectionRequest.VSLiveShareApiEndpoint);
        }
    }
}
