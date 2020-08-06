// <copyright file="MockEnvironmentMonitor.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Mocks
{
    /// <summary>
    /// Mock environment monitor.
    /// </summary>
    public class MockEnvironmentMonitor : IEnvironmentMonitor
    {
        /// <inheritdoc/>
        public Task MonitorHeartbeatAsync(string environmentId, Guid? computeId, IDiagnosticsLogger logger)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task MonitorProvisioningStateTransitionAsync(string environmentId, Guid computeId, IDiagnosticsLogger logger)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task MonitorProvisioningStateTransitionAsync(string environmentId, Guid computeId, TimeSpan timeout, IDiagnosticsLogger logger)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task MonitorResumeStateTransitionAsync(string environmentId, Guid computeId, IDiagnosticsLogger logger)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task MonitorShutdownStateTransitionAsync(string environmentId, Guid computeId, IDiagnosticsLogger logger)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task MonitorUnavailableStateTransitionAsync(string environmentId, Guid computeId, IDiagnosticsLogger logger)
        {
            return Task.CompletedTask;
        }
    }
}
