// <copyright file="IJobSchedulerLease.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// A job scheduler lease contract to allow jobs to be registered and executed only if a lease is obtained.
    /// </summary>
    public interface IJobSchedulerLease
    {
        /// <summary>
        /// Add a recurring job that will use a lease to conditionally run a job.
        /// </summary>
        /// <param name="expression">Cron expression.</param>
        /// <param name="jobName">The job name.</param>
        /// <param name="queueName">Job Queue name to use.</param>
        /// <param name="claimSpan">Time to hold the claim.</param>
        /// <param name="jobSchedulePayloadFactory">A factory instance to return all the job payloads.</param>
        /// <param name="isEnabledCallback">Optional callback to evalaute if the recurring job is enabled.</param>
        /// <returns>A scheduled job instance.</returns>
        IScheduleJob AddRecurringJobPayload(
            string expression,
            string jobName,
            string queueName,
            TimeSpan claimSpan,
            IJobSchedulePayloadFactory jobSchedulePayloadFactory,
            Func<DateTime, Task<bool>> isEnabledCallback = null);
    }

    /// <summary>
    /// Contract to obtain a lease.
    /// </summary>
    public interface IJobSchedulerLeaseProvider
    {
        /// <summary>
        /// Obtain a lease base on a job name.
        /// </summary>
        /// <param name="jobName">The job name that want to obtain a lease.</param>
        /// <param name="timeSpan">Time to hold the lease.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A disposable object or null if the lease wasn't obtained.</returns>
        Task<IDisposable> ObtainAsync(string jobName, TimeSpan timeSpan, IDiagnosticsLogger logger, CancellationToken cancellationToken);
    }
}
