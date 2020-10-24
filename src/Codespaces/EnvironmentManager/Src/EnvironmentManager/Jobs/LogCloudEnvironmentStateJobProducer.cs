// <copyright file="LogCloudEnvironmentStateJobProducer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Jobs
{
    public class LogCloudEnvironmentStateJobProducer : IJobSchedulePayloadFactory, IJobSchedulerRegister
    {
        public const string FeatureFlagName = "EnvironmentManagerJob";

        public const string JobName = "log_cloud_environment_state_job";

        public const string QueueName = EnvironmentJobQueueConstants.GenericQueueName;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogCloudEnvironmentStateJobProducer"/> class.
        /// <param name="jobSchedulerFeatureFlags">Job scheduler feature flags</param>
        /// </summary>
        public LogCloudEnvironmentStateJobProducer(IJobSchedulerFeatureFlags jobSchedulerFeatureFlags)
        {
            JobSchedulerFeatureFlags = jobSchedulerFeatureFlags;
        }

        private (string CronExpression, TimeSpan Interval) ScheduleTimeInterval => JobPayloadRegisterSchedule.LogCloudEnvironmentStateJobSchedule;

        private IJobSchedulerFeatureFlags JobSchedulerFeatureFlags { get; }

        public Task CreatePayloadsAsync(string jobRunId, DateTime scheduleRun, IServiceProvider serviceProvider, OnPayloadsCreatedDelegateAsync onCreated, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            var jobPayload = new LogCloudEnvironmentStateJobHandler.Payload();
            var jobPayloadOptions = new JobPayloadOptions()
            {
                ExpireTimeout = JobPayloadOptions.DefaultJobPayloadExpireTimeout,
            };

            return onCreated.AddPayloadAsync(jobPayload, jobPayloadOptions);
        }

        public void RegisterScheduleJob()
        {
            JobSchedulerFeatureFlags.AddRecurringJobPayload(
                ScheduleTimeInterval.CronExpression,
                jobName: $"{JobName}_run",
                QueueName,
                claimSpan: ScheduleTimeInterval.Interval,
                this,
                FeatureFlagName);
        }
    }
}
