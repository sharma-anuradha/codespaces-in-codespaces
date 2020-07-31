// <copyright file="IJobSchedulePayloadFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// Contract to define a job scheduler payload factory.
    /// </summary>
    public interface IJobSchedulePayloadFactory
    {
        /// <summary>
        /// Create job payloads that will be added into a job queue.
        /// </summary>
        /// <param name="jobRunId">Job run identifier.</param>
        /// <param name="scheduleRun">When the job scheduler run this job.</param>
        /// <param name="serviceProvider">Instance of a service provider.</param>
        /// <param name="logger">Diganostic logger instance.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completion task with list of job payloads.</returns>
        Task<IEnumerable<(JobPayload, JobPayloadOptions)>> CreatePayloadsAsync(string jobRunId, DateTime scheduleRun, IServiceProvider serviceProvider, IDiagnosticsLogger logger, CancellationToken cancellationToken);
    }
}
