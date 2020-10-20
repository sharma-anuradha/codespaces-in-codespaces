// <copyright file="SystemConfigurationMigrationJobProducer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks.SystemConfigurationMigration
{
    /// <summary>
    /// A class that implements IJobSchedulePayloadFactory and able to produce job payloads
    /// for system configuration migration task
    /// </summary>
    public class SystemConfigurationMigrationJobProducer : IJobSchedulePayloadFactory, IJobSchedulerRegister
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemConfigurationMigrationJobProducer"/> class.
        /// <param name="jobSchedulerFeatureFlags">Job scheduler feature flags</param>
        /// </summary>
        public SystemConfigurationMigrationJobProducer(IJobSchedulerFeatureFlags jobSchedulerFeatureFlags)
        {
            JobSchedulerFeatureFlags = jobSchedulerFeatureFlags;
        }

        private string JobName => "system_configuration_migration_task";

        // Run once every 1 hour
        private (string CronExpression, TimeSpan Interval) ScheduleTimeInterval => JobPayloadRegisterSchedule.SystemConfigurationMigrationJobSchedule;

        private IJobSchedulerFeatureFlags JobSchedulerFeatureFlags { get; }

        public Task CreatePayloadsAsync(string jobRunId, DateTime scheduleRun, IServiceProvider serviceProvider, OnPayloadsCreatedDelegateAsync onPayloadCreated, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            var jobPayload = new SystemConfigurationMigrationPayload();
            var jobPayloadOptions = new JobPayloadOptions()
            {
                ExpireTimeout = TimeSpan.FromMinutes(20),
            };

            return onPayloadCreated.AddPayloadAsync(jobPayload, jobPayloadOptions);
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
        /// A system configuration migration payload.
        /// </summary>
        public class SystemConfigurationMigrationPayload : JobPayload
        {
        }
    }
}
