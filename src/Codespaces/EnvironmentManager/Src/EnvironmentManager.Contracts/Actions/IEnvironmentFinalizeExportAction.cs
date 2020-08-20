// <copyright file="IEnvironmentFinalizeExportAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment Finalize Resume Action.
    /// </summary>
    public interface IEnvironmentFinalizeExportAction : IEnvironmentItemAction<EnvironmentFinalizeExportActionInput, object>
    {
        /// <summary>
        /// Run action that completes the start of a shutdown environment.
        /// </summary>
        /// <param name="environmentId">Target environment Id.</param>
        /// <param name="storageResourceId">Target new storage that should be swapped in.</param>
        /// <param name="archiveStorageResourceId">Target archive storage resource id if waking from archive.</param>
        /// <param name="exportedEnvironmentUrl">Exported environment url.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The resumed environment.</returns>
        Task<CloudEnvironment> RunAsync(
            Guid environmentId,
            Guid storageResourceId,
            Guid? archiveStorageResourceId,
            string exportedEnvironmentUrl,
            IDiagnosticsLogger logger);
    }
}