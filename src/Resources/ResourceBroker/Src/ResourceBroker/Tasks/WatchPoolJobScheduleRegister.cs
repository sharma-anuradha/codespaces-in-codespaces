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
        /// Initializes a new instance of the <see cref="WatchPoolJobScheduleRegister"/> class.
        /// </summary>
        /// <param name="jobSchedulerLease"> A job scheduler lease instance.</param>
        /// <param name="watchPoolPayloadFactory">A watch pool payload factory instance.</param>
        public WatchPoolJobScheduleRegister(
            IJobSchedulerLease jobSchedulerLease,
            WatchPoolPayloadFactory watchPoolPayloadFactory)
        {
            JobSchedulerLease = jobSchedulerLease;
            WatchPoolPayloadFactory = watchPoolPayloadFactory;
        }

        private IJobSchedulerLease JobSchedulerLease { get; }

        private WatchPoolPayloadFactory WatchPoolPayloadFactory { get; }

        /// <inheritdoc/>
        public void RegisterScheduleJob()
        {
            JobSchedulerLease.AddRecurringJobPayload(
                "* * * * *",
                $"{ResourceLoggingConstants.WatchPoolProducerTask}_run",
                ResourceJobQueueConstants.GenericQueueName,
                claimSpan: TimeSpan.FromMinutes(1),
                WatchPoolPayloadFactory);
        }
    }
}
