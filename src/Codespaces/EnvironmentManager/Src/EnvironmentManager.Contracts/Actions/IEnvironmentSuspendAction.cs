// <copyright file="IEnvironmentSuspendAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment suspend action.
    /// </summary>
    public interface IEnvironmentSuspendAction : IEnvironmentItemAction<EnvironmentSuspendActionInput, object>
    {
        /// <summary>
        /// Suspend an environment.
        /// </summary>
        /// <param name="environmentId">Target environment Id.</param>
        /// <param name="computeResourceId">Target compute resource Id, allocated to the environment that is not persisted yet.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns run result from action.</returns>
        Task<CloudEnvironment> Run(Guid environmentId, Guid computeResourceId, IDiagnosticsLogger logger);

        /// <summary>
        /// Suspend an environment.
        /// </summary>
        /// <param name="environmentId">Target environment Id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns run result from action.</returns>
        Task<CloudEnvironment> Run(Guid environmentId, IDiagnosticsLogger logger);
    }
}
