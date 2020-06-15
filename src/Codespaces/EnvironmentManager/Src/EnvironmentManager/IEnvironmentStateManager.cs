// <copyright file="IEnvironmentStateManager.cs" company="Microsoft">
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
        /// <param name="logger">target logger.</param>
        /// <returns>task.</returns>
        Task SetEnvironmentStateAsync(
          CloudEnvironment cloudEnvironment,
          CloudEnvironmentState state,
          string trigger,
          string reason,
          IDiagnosticsLogger logger);
    }
}
