// <copyright file="IEnvironmentForceSuspendAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment force suspend action.
    /// </summary>
    public interface IEnvironmentForceSuspendAction : IEnvironmentItemAction<EnvironmentSuspendActionInput, object>
    {
        /// <summary>
        /// Run environment force suspend action for the given environemt.
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns run result from action.</returns>
        Task<CloudEnvironment> RunAsync(Guid environmentId, IDiagnosticsLogger logger);
    }
}
