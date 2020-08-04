// <copyright file="IEnvironmentCreateAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment Update Status Action.
    /// </summary>
    public interface IEnvironmentCreateAction : IEnvironmentItemAction<EnvironmentCreateActionInput, EnvironmentCreateTransientState>
    {
        /// <summary>
        /// Run environment create action.
        /// </summary>
        /// <param name="details">Target options.</param>
        /// <param name="startEnvironmentParams">Target environment params.</param>
        /// <param name="metricsInfo">Target metrics info.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns run result from the create action.</returns>
        Task<CloudEnvironment> RunAsync(
            EnvironmentCreateDetails details,
            StartCloudEnvironmentParameters startEnvironmentParams,
            MetricsInfo metricsInfo,
            IDiagnosticsLogger logger);
    }
}
