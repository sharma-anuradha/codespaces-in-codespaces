// <copyright file="IEnvironmentInitializeUpdateAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment initialize update action.
    /// </summary>
    public interface IEnvironmentInitializeUpdateAction : IEnvironmentBaseIntializeStartAction<EnvironmentUpdateActionInput>
    {
        /// <summary>
        /// Initialize environment update action.
        /// </summary>
        /// <param name="environmentId">Environment ID.</param>
        /// <param name="cloudEnvironmentParams">Start compute arguments.</param>
        /// <param name="logger">Diagnostics logger</param>
        /// <returns>Cloud environment.</returns>
        Task<CloudEnvironment> RunAsync(
            Guid environmentId,
            CloudEnvironmentParameters cloudEnvironmentParams,
            IDiagnosticsLogger logger);
    }
}
