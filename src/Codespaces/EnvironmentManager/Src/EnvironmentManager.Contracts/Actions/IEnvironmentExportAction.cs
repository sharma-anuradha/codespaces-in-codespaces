// <copyright file="IEnvironmentExportAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment Export Action.
    /// </summary>
    public interface IEnvironmentExportAction : IEnvironmentItemAction<EnvironmentExportActionInput, EnvironmentExportTransientState>
    {
        /// <summary>
        /// Run environment export action.
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        /// <param name="exportEnvironmentParams">Target export environment params.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns run result from the create action.</returns>
        Task<CloudEnvironment> RunAsync(
            Guid environmentId,
            ExportCloudEnvironmentParameters exportEnvironmentParams,
            IDiagnosticsLogger logger);
    }
}
