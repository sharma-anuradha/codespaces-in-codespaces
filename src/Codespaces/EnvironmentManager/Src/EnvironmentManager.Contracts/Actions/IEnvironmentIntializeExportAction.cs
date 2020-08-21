// <copyright file="IEnvironmentIntializeExportAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment Intialize Resume Action.
    /// </summary>
    public interface IEnvironmentIntializeExportAction : IEnvironmentBaseIntializeStartAction<EnvironmentExportActionInput>
    {
        /// <summary>
        /// Initialize environment export action.
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        /// <param name="exportEnvironmentParams">Target environment params.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns run result from the create action.</returns>
        Task<CloudEnvironment> RunAsync(
            Guid environmentId,
            ExportCloudEnvironmentParameters exportEnvironmentParams,
            IDiagnosticsLogger logger);
    }
}
