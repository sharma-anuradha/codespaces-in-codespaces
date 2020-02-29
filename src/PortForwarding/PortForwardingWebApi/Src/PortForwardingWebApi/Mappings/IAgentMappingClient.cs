// <copyright file="IAgentMappingClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Mappings
{
    /// <summary>
    /// A client for managing connection mappings.
    /// </summary>
    public interface IAgentMappingClient
    {
        /// <summary>
        /// Creates the external connection mapping. This involves creating a kubernetes service and a kubernetes ingress rule to route the traffic properly.
        /// </summary>
        /// <param name="mapping">Connection mapping description.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Task.</returns>
        Task CreateAgentConnectionMappingAsync(ConnectionEstablished mapping, IDiagnosticsLogger logger);

        /// <summary>
        /// Ensures the agent has all the labels required for routing traffic to it.
        /// </summary>
        /// <param name="registration">Agent registration details.</param>
        /// <param name="logger">Target Logger.</param>
        /// <returns>Task.</returns>
        Task RegisterAgentAsync(AgentRegistration registration, IDiagnosticsLogger logger);

        /// <summary>
        /// Removes the agent from deployment so Kubernetes can autoscale the deployments properly.
        /// </summary>
        /// <param name="agentName">Agent name.</param>
        /// <param name="logger">Target Logger.</param>
        /// <returns>Task.</returns>
        Task RemoveBusyAgentFromDeploymentAsync(string agentName, IDiagnosticsLogger logger);
    }
}