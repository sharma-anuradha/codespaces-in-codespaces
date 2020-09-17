// <copyright file="WatchPoolJobScheduleRegister.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Job scheduler registration entry point.
    /// </summary>
    public class WatchPoolJobScheduleRegister : IJobSchedulerRegister
    {
        /// <summary>
        /// Feature flag to control whether the job pools are enabled.
        /// </summary>
        public const string WatchPoolJobsEnabledFeatureFlagName = "WatchPoolJobs";

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchPoolJobScheduleRegister"/> class.
        /// </summary>
        /// <param name="watchPoolPayloadFactory">A watch pool payload factory instance.</param>
        /// <param name="jobSchedulerFeatureFlags">The job scheduler feature flags instance.</param>
        public WatchPoolJobScheduleRegister(
            WatchPoolPayloadFactory watchPoolPayloadFactory,
            IJobSchedulerFeatureFlags jobSchedulerFeatureFlags)
        {
            WatchPoolPayloadFactory = watchPoolPayloadFactory;
            JobSchedulerFeatureFlags = jobSchedulerFeatureFlags;
        }

        private WatchPoolPayloadFactory WatchPoolPayloadFactory { get; }

        private IJobSchedulerFeatureFlags JobSchedulerFeatureFlags { get; }

        /// <inheritdoc/>
        public void RegisterScheduleJob()
        {
            JobSchedulerFeatureFlags.AddRecurringJobPayload(
                "* * * * *",
                $"{ResourceLoggingConstants.WatchPoolProducerTask}_run",
                ResourceJobQueueConstants.GenericQueueName,
                claimSpan: TimeSpan.FromMinutes(1),
                WatchPoolPayloadFactory,
                WatchPoolJobsEnabledFeatureFlagName);
        }
    }
}
