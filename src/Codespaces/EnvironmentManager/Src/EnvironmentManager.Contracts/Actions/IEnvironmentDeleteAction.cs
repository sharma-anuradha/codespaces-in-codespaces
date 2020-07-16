// <copyright file="IEnvironmentDeleteAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts.Actions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment Delete Action.
    /// </summary>
    public interface IEnvironmentDeleteAction : IEnvironmentBaseItemAction<EnvironmentDeleteActionInput, bool>
    {
        /// <summary>
        /// Delete cloud environment by id.
        /// </summary>
        /// <param name="cloudEnvironmentId">Target cloud environment id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>True if the environment was deleted, otherwise false.</returns>
        Task<bool> Run(string cloudEnvironmentId, IDiagnosticsLogger logger);

        /// <summary>
        /// Delete given cloud environment.
        /// </summary>
        /// <param name="cloudEnvironment">Target cloud environment id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>True if the environment was deleted, otherwise false.</returns>
        Task<bool> Run(CloudEnvironment cloudEnvironment, IDiagnosticsLogger logger);
    }
}
