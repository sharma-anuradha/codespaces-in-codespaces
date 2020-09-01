// <copyright file="IEnvironmentStateManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// State Transition Manager.
    /// </summary>
    public interface IEnvironmentStateManager
    {
        /// <summary>
        /// Validates state transitions, generates required events.
        /// </summary>
        /// <param name="cloudEnvironment">Target cloud environment.</param>
        /// <param name="newState">target state.</param>
        /// <param name="trigger">target trigger.</param>
        /// <param name="reason">target reason.</param>
        /// <param name="isUserError">Is user generated error.</param>
        /// <param name="logger">target logger.</param>
        /// <returns>task.</returns>
        Task SetEnvironmentStateAsync(
          CloudEnvironment cloudEnvironment,
          CloudEnvironmentState newState,
          string trigger,
          string reason,
          bool? isUserError,
          IDiagnosticsLogger logger);

        /// <summary>
        /// Validates state tranisions, generates required events,
        /// and apply mutations as replayable transitions.
        /// </summary>
        /// <param name="record">Target cloud environment transition.</param>
        /// <param name="newState">target state.</param>
        /// <param name="trigger">target trigger.</param>
        /// <param name="reason">target reason.</param>
        /// <param name="isUserError">Is user generated error.</param>
        /// <param name="logger">target logger.</param>
        /// <returns>task.</returns>
        Task SetEnvironmentStateAsync(
            EnvironmentTransition record,
            CloudEnvironmentState newState,
            string trigger,
            string reason,
            bool? isUserError,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Normalizes environment state if there are any inconsistencies.
        /// </summary>
        /// <param name="cloudEnvironment">Target cloud environement.</param>
        /// <param name="checkWorkspaceStatus">Feature flag enabled to check the workspace status.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Updated cloud environment if there is any updates.</returns>
        Task<CloudEnvironment> NormalizeEnvironmentStateAsync(
          CloudEnvironment cloudEnvironment,
          bool checkWorkspaceStatus,
          IDiagnosticsLogger logger);
    }
}
