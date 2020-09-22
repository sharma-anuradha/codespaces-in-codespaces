// <copyright file="LogSystemResourceStateJobProducer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// A class that implements IJobSchedulePayloadFactory and able to produce job payloads
    /// for logging system resource state
    /// </summary>
    public class LogSystemResourceStateJobProducer : IJobSchedulePayloadFactory, IJobSchedulerRegister
    {
        public LogSystemResourceStateJobProducer(IJobSchedulerFeatureFlags jobSchedulerFeatureFlags)
        {
            JobSchedulerFeatureFlags = jobSchedulerFeatureFlags;
        }

        protected string JobName { get; }

        protected Type JobHandlerType { get; }

        private IJobSchedulerFeatureFlags JobSchedulerFeatureFlags { get; }

        public Task CreatePayloadsAsync(string jobRunId, DateTime scheduleRun, IServiceProvider serviceProvider, OnPayloadCreatedDelegate onPayloadCreated, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            var jobPayload = new LogSystemResourceStatePayload();
            var jobPayloadOptions = new JobPayloadOptions()
            {
                ExpireTimeout = JobPayloadOptions.DefaultJobPayloadExpireTimeout,
            };

            return onPayloadCreated(jobPayload, jobPayloadOptions);
        }

        public void RegisterScheduleJob()
        {
            JobSchedulerFeatureFlags.AddRecurringJobPayload(
                "*/10 * * * *",
                jobName: JobName,
                ResourceJobQueueConstants.GenericQueueName,
                claimSpan: TimeSpan.FromMinutes(10),
                this,
                null);
        }

        /// <summary>
        /// A log system resource state payload.
        /// </summary>
        public class LogSystemResourceStatePayload : JobPayload
        {
        }
    }
}
