// <copyright file="IRunScheduleJob.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts
{
    /// <summary>
    /// Contract to define a handler of a scheduled job.
    /// </summary>
    public interface IRunScheduleJob
    {
        /// <summary>
        /// Gets the name of this schedule job.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Callback when a schedule job is executed.
        /// </summary>
        /// <param name="jobRunId">Job run identifier.</param>
        /// <param name="scheduleRun">When the job scheduler run this job.</param>
        /// <param name="serviceProvider">Instance of a service provider.</param>
        /// <param name="logger">Diganostic logger instance.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completion task.</returns>
        Task RunAsync(string jobRunId, DateTime scheduleRun, IServiceProvider serviceProvider, IDiagnosticsLogger logger, CancellationToken cancellationToken);
    }
}
