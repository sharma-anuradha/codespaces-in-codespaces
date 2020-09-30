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
        /// <summary>
        /// Initializes a new instance of the <see cref="LogSystemResourceStateJobProducer"/> class.
        /// <param name="jobSchedulerFeatureFlags">Job scheduler feature flags</param>
        /// </summary>
        public LogSystemResourceStateJobProducer(IJobSchedulerFeatureFlags jobSchedulerFeatureFlags)
        {
            JobSchedulerFeatureFlags = jobSchedulerFeatureFlags;
        }

        private string JobName => "log_system_resource_state_task";

        // Run once every 10 minutes
        private (string CronExpression, TimeSpan Interval) ScheduleTimeInterval => JobPayloadRegisterSchedule.LogSystemResourceStateJobSchedule;

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
                ScheduleTimeInterval.CronExpression,
                jobName: $"{JobName}_run",
                ResourceJobQueueConstants.GenericQueueName,
                claimSpan: ScheduleTimeInterval.Interval,
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
