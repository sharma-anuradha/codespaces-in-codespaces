// <copyright file="IEnvironmentMonitor.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment monitor operations.
    /// </summary>
    public interface IEnvironmentMonitor
    {
        /// <summary>
        /// Monitor environment heartbeat by invoking the continution activator.
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        /// <param name="computeId">Target compute id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>returns task.</returns>
        Task MonitorHeartbeatAsync(
                string environmentId,
                Guid? computeId,
                IDiagnosticsLogger logger);

        /// <summary>
        /// Monitors the first response back from the agent indicating that provisioning has started.
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        /// <param name="computeId">Target compute id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>returns task.</returns>
        Task MonitorProvisioningStateTransitionAsync(
            string environmentId,
            Guid computeId,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Monitors the first response back from the agent indicating that provisioning has started.
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        /// <param name="computeId">Target compute id.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>returns task.</returns>
        Task MonitorProvisioningStateTransitionAsync(
            string environmentId,
            Guid computeId,
            TimeSpan timeout,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Monitor the unavailable state transition by invoking the continution activator.
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        /// <param name="computeId">Target compute id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>returns task.</returns>
        Task MonitorUnavailableStateTransitionAsync(
            string environmentId,
            Guid computeId,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Monitor the resume state transition by invoking the continution activator.
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        /// <param name="computeId">Target compute id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>returns task.</returns>
        Task MonitorResumeStateTransitionAsync(
            string environmentId,
            Guid computeId,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Monitor the shutdown state transition by invoking the continution activator.
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        /// <param name="computeId">Target compute id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>returns task.</returns>
        Task MonitorShutdownStateTransitionAsync(
            string environmentId,
            Guid computeId,
            IDiagnosticsLogger logger);
    }
}
