// <copyright file="IEnvironmentUpdateAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment update action.
    /// </summary>
    public interface IEnvironmentUpdateAction : IEnvironmentItemAction<EnvironmentUpdateActionInput, EnvironmentExportTransientState>
    {
        /// <summary>
        /// Run environment update action.
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        /// <param name="cloudEnvironmentParams">Target export environment params.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns run result from the create action.</returns>
        Task<CloudEnvironment> RunAsync(
            Guid environmentId,
            CloudEnvironmentParameters cloudEnvironmentParams,
            IDiagnosticsLogger logger);
    }
}
