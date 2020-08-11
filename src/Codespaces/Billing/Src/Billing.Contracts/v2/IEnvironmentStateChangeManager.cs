// <copyright file="IEnvironmentStateChangeManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts
{
    /// <summary>
    /// The Environment state change manager's interface.
    /// </summary>
    public interface IEnvironmentStateChangeManager
    {
        /// <summary>
        /// Gets all environment state changes withing a given window.
        /// </summary>
        /// <param name="planId">the planId for all environment state changes.</param>
        /// <param name="startTime">the starting time.</param>
        /// <param name="endTime">the ending time.</param>
        /// <param name="logger">the logger.</param>
        /// <returns>a list of all environment state transitions that occured during.</returns>
        Task<IEnumerable<EnvironmentStateChange>> GetAllRecentEnvironmentEvents(string planId, DateTime startTime, DateTime endTime, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets all state changes up to a given time.
        /// </summary>
        /// <param name="planId">planId.</param>
        /// <param name="endTime">the ending time.</param>
        /// <param name="logger">the logger.</param>
        /// <returns>Task.</returns>
        Task<IEnumerable<EnvironmentStateChange>> GetAllStateChanges(string planId, DateTime endTime, IDiagnosticsLogger logger);

        /// <summary>
        /// Creates the state change in the repository.
        /// </summary>
        /// <param name="change">the new enviironment state change to add.</param>
        /// <param name="logger">the logger.</param>
        /// <returns>A task indicating completion.</returns>
        Task CreateAsync(EnvironmentStateChange change, IDiagnosticsLogger logger);

        /// <summary>
        /// Builds and saves a state change in the repository.
        /// </summary>
        /// <param name="plan">The plan info.</param>
        /// <param name="environment">The environment info.</param>
        /// <param name="oldState">The old state.</param>
        /// <param name="newState">The new state.</param>
        /// <param name="logger">The logger instance.</param>
        /// <returns>Task.</returns>
        Task CreateAsync(VsoPlanInfo plan, EnvironmentBillingInfo environment, CloudEnvironmentState oldState, CloudEnvironmentState newState, IDiagnosticsLogger logger);
    }
}
