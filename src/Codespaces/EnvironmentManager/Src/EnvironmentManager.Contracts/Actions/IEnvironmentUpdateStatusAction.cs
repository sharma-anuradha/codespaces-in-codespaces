// <copyright file="IEnvironmentUpdateStatusAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment Update Status Action.
    /// </summary>
    public interface IEnvironmentUpdateStatusAction : IEnvironmentItemAction<EnvironmentUpdateStatusActionInput>
    {
        /// <summary>
        /// Update environment status.
        /// </summary>
        /// <param name="cloudEnvironmentId">Target cloud environment's Id.</param>
        /// <param name="newState">Target new state.</param>
        /// <param name="trigger">Target trigger.</param>
        /// <param name="reason">Target reason.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns run result from the update status action.</returns>
        Task<CloudEnvironment> Run(
            Guid cloudEnvironmentId,
            CloudEnvironmentState newState,
            string trigger,
            string reason,
            IDiagnosticsLogger logger);
    }
}
