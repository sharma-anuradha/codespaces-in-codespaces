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
                Guid computeId,
                IDiagnosticsLogger logger);

        /// <summary>
        /// Monitor environment heartbeat by invoking the continution activator.
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        /// <param name="computeId">Target compute id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>returns task.</returns>
        Task MonitorUnavailableStateTransition(
            string environmentId,
            Guid computeId,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Monitor environment heartbeat by invoking the continution activator.
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        /// <param name="computeId">Target compute id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>returns task.</returns>
        Task MonitorResumeStateTransitionAsync(
            string environmentId,
            Guid computeId,
            IDiagnosticsLogger logger);
    }
}
