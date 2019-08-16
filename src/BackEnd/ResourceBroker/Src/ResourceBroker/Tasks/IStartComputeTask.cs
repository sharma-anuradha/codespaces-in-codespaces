// <copyright file="IStartComputeTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Task which starts compute resource.
    /// </summary>
    public interface IStartComputeTask
    {
        /// <summary>
        /// Start compute with storage and startup parameters.
        /// </summary>
        /// <param name="input">Environment start inputs.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="continuationToken">Token used to continue the task.</param>
        /// <returns>A task.</returns>
        Task<EnvironmentStartResult> RunAsync(
            EnvironmentStartInput input,
            IDiagnosticsLogger logger,
            string continuationToken = null);
    }
}
