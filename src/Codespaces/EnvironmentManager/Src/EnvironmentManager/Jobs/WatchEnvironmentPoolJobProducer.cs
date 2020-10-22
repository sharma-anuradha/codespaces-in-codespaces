// <copyright file="WatchEnvironmentPoolJobProducer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Jobs
{
    /// <summary>
    /// Job scheduler registration entry point.
    /// </summary>
    public class WatchEnvironmentPoolJobProducer : IJobSchedulerRegister
    {
        /// <summary>
        /// Feature flag to control whether the job pools are enabled.
        /// </summary>
        public const string WatchPoolJobsEnabledFeatureFlagName = "WatchEnvironmentPoolJobs";

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchEnvironmentPoolJobProducer"/> class.
        /// </summary>
        /// <param name="watchPoolPayloadFactory">A watch pool payload factory instance.</param>
        /// <param name="jobSchedulerFeatureFlags">The job scheduler feature flags instance.</param>
        public WatchEnvironmentPoolJobProducer(
            WatchEnvironmentPoolPayloadFactory watchPoolPayloadFactory,
            IJobSchedulerFeatureFlags jobSchedulerFeatureFlags)
        {
            WatchPoolPayloadFactory = Requires.NotNull(watchPoolPayloadFactory, nameof(watchPoolPayloadFactory));
            JobSchedulerFeatureFlags = Requires.NotNull(jobSchedulerFeatureFlags, nameof(jobSchedulerFeatureFlags));
        }

        private WatchEnvironmentPoolPayloadFactory WatchPoolPayloadFactory { get; }

        private IJobSchedulerFeatureFlags JobSchedulerFeatureFlags { get; }

        // run every minute
        private (string CronExpression, TimeSpan Interval) ScheduleTimeInterval => JobPayloadRegisterSchedule.WatchPoolJobSchedule;

        /// <inheritdoc/>
        public void RegisterScheduleJob()
        {
            JobSchedulerFeatureFlags.AddRecurringJobPayload(
                ScheduleTimeInterval.CronExpression,
                $"{EnvironmentLoggingConstants.WatchPoolProducerTask}_run",
                EnvironmentJobQueueConstants.GenericQueueName,
                claimSpan: ScheduleTimeInterval.Interval,
                WatchPoolPayloadFactory,
                WatchPoolJobsEnabledFeatureFlagName);
        }
    }
}
