// <copyright file="IEnvironmentDeleteAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts.Actions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment Delete Action.
    /// </summary>
    public interface IEnvironmentDeleteAction : IEnvironmentBaseItemAction<EnvironmentDeleteActionInput, object, bool>
    {
        /// <summary>
        /// Delete cloud environment by id.
        /// </summary>
        /// <param name="cloudEnvironmentId">Target cloud environment id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>True if the environment was deleted, otherwise false.</returns>
        Task<bool> Run(Guid cloudEnvironmentId, IDiagnosticsLogger logger);

        /// <summary>
        /// Delete given cloud environment and the resources which might not yet be persisted.
        /// </summary>
        /// <param name="cloudEnvironmentId">Target cloud environment id.</param>
        /// <param name="computeResourceId">Target compute id to be de-allocated.</param>
        /// <param name="storageResourceId">Target storage id to be de-allocated.</param>
        /// <param name="osDiskResourceId">Target os disk id to be de-allocated.</param>
        /// <param name="liveshareWorkspaceId">Target liveshare workspace id to be deleted.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>True if the environment was deleted, otherwise false.</returns>
        Task<bool> Run(Guid cloudEnvironmentId, Guid? computeResourceId, Guid? storageResourceId, Guid? osDiskResourceId, string liveshareWorkspaceId, IDiagnosticsLogger logger);
    }
}
