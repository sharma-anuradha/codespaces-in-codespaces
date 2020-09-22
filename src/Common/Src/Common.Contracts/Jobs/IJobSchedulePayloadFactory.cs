// <copyright file="IJobSchedulePayloadFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    public delegate Task OnPayloadCreatedDelegate(JobPayload payload, JobPayloadOptions options);

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
        /// <param name="onCreated">Callback to be invoked when a new payload is ready.</param>
        /// <param name="logger">Diganostic logger instance.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completion task with list of job payloads.</returns>
        Task CreatePayloadsAsync(
            string jobRunId,
            DateTime scheduleRun,
            IServiceProvider serviceProvider,
            OnPayloadCreatedDelegate onCreated,
            IDiagnosticsLogger logger,
            CancellationToken cancellationToken);
    }
}
