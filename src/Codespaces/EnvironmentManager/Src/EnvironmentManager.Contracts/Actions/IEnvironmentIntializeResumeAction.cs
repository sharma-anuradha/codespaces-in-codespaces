// <copyright file="IEnvironmentIntializeResumeAction.cs" company="Microsoft">
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
    public interface IEnvironmentIntializeResumeAction : IEnvironmentBaseIntializeStartAction<EnvironmentResumeActionInput>
    {
        /// <summary>
        /// Intialize environment resume action.
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        /// <param name="startEnvironmentParams">Target environment params.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns run result from the create action.</returns>
        Task<CloudEnvironment> RunAsync(
            Guid environmentId,
            StartCloudEnvironmentParameters startEnvironmentParams,
            IDiagnosticsLogger logger);
    }
}
