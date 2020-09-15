// <copyright file="IJobHandlerErrorCallback.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// Interface callback when a job handler throws an error.
    /// </summary>
    public interface IJobHandlerErrorCallback
    {
        /// <summary>
        /// Handle an exception from a user job handler
        /// </summary>
        /// <param name="job">The job instance.</param>
        /// <param name="error">Exception error being handled.</param>
        /// <param name="status">Input job status.</param>
        /// <param name="logger">A logger diagnostic instance.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A custom status from this handler error.</returns>
        Task<JobCompletedStatus> HandleJobError(IJob job, Exception error, JobCompletedStatus status, IDiagnosticsLogger logger, CancellationToken cancellationToken);
    }
}
