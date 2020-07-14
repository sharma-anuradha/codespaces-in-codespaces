// <copyright file="NullAgentMappingClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using k8s.Models;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Mappings
{
    /// <inheritdoc/>
    public class NullAgentMappingClient : IAgentMappingClient
    {
        /// <inheritdoc/>
        public Task RegisterAgentAsync(AgentRegistration registration, IDiagnosticsLogger logger)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task CreateAgentConnectionMappingAsync(ConnectionDetails mapping, IDiagnosticsLogger logger)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RemoveBusyAgentFromDeploymentAsync(string agentName, IDiagnosticsLogger logger)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task KillAgentAsync(string agentName, IDiagnosticsLogger logger)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<V1Service> WaitForServiceAvailableAsync(string serviceName, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<V1Service> WaitForServiceAvailableAsync(string serviceName, TimeSpan timeout, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Extensionsv1beta1Ingress> WaitForIngressReadyAsync(string serviceName, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Extensionsv1beta1Ingress> WaitForIngressReadyAsync(string serviceName, TimeSpan timeout, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<V1Endpoints> WaitForEndpointReadyAsync(string endpointName, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<V1Endpoints> WaitForEndpointReadyAsync(string endpointName, TimeSpan timeout, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }
    }
}
