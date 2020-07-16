﻿// <copyright file="IEnvironmentStateManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// State Transition Manager.
    /// </summary>
    public interface IEnvironmentStateManager
    {
        /// <summary>
        /// Validates state tranisions, generates required events.
        /// </summary>
        /// <param name="cloudEnvironment">Target cloud environment.</param>
        /// <param name="state">target state.</param>
        /// <param name="trigger">target trigger.</param>
        /// <param name="reason">target reason.</param>
        /// <param name="isUserError">Is user generated error.</param>
        /// <param name="logger">target logger.</param>
        /// <returns>task.</returns>
        Task SetEnvironmentStateAsync(
          CloudEnvironment cloudEnvironment,
          CloudEnvironmentState state,
          string trigger,
          string reason,
          bool? isUserError,
          IDiagnosticsLogger logger);

        /// <summary>
        /// Normalizes environment state if there are any inconsistencies.
        /// </summary>
        /// <param name="cloudEnvironment">Target cloud environement.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Updated cloud environment if there is any updates.</returns>
        Task<CloudEnvironment> NormalizeEnvironmentStateAsync(
          CloudEnvironment cloudEnvironment,
          IDiagnosticsLogger logger);
    }
}
