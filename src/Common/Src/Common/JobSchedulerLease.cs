// <copyright file="JobSchedulerLease.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Implements IJobSchedulerLease interface.
    /// </summary>
    public class JobSchedulerLease : IJobSchedulerLease
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Common.JobSchedulerLease"/> class.
        /// </summary>
        /// <param name="jobScheduler">A job scheduler instance.</param>
        /// <param name="jobSchedulerLeaseProvider">A job scheduler lease provider instance.</param>
        /// <param name="jobQueueProducerFactory">A job queue producer factory instance.</param>
        public JobSchedulerLease(
            IJobScheduler jobScheduler,
            IJobSchedulerLeaseProvider jobSchedulerLeaseProvider,
            IJobQueueProducerFactory jobQueueProducerFactory)
        {
            JobScheduler = Requires.NotNull(jobScheduler, nameof(jobScheduler));
            JobSchedulerLeaseProvider = Requires.NotNull(jobSchedulerLeaseProvider, nameof(jobSchedulerLeaseProvider));
            JobQueueProducerFactory = Requires.NotNull(jobQueueProducerFactory, nameof(jobQueueProducerFactory));
        }

        private IJobScheduler JobScheduler { get; }

        private IJobSchedulerLeaseProvider JobSchedulerLeaseProvider { get; }

        private IJobQueueProducerFactory JobQueueProducerFactory { get; }

        /// <inheritdoc/>
        public IScheduleJob AddRecurringJobPayload(
            string expression,
            string jobName,
            string queueName,
            TimeSpan claimSpan,
            IJobSchedulePayloadFactory jobSchedulePayloadFactory,
            Func<DateTime, Task<bool>> isEnabledCallback)
        {
            return JobScheduler.AddRecurringJobPayload(
                expression,
                jobName,
                JobQueueProducerFactory.GetOrCreate(queueName),
                JobSchedulerProducerHelpers.CreateJobSchedulePayloadFactory(async (jobRunId, dt, srvcProvider, onCreated, logger, ct) =>
                {
                    if (isEnabledCallback == null || await isEnabledCallback(dt))
                    {
                        using (var lease = await JobSchedulerLeaseProvider.ObtainAsync(jobName, claimSpan, logger, ct))
                        {
                            logger.AddValue("jobRunLeaseObtaioned", (lease != null).ToString());
                            if (lease != null)
                            {
                                await jobSchedulePayloadFactory.CreatePayloadsAsync(jobRunId, dt, srvcProvider, onCreated, logger, ct);                                
                            }
                        }
                    }
                }));
        }
    }
}
