// <copyright file="EnvironmentStateRepairJobProducer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    public class EnvironmentStateRepairJobProducer : IJobSchedulerRegister, IJobSchedulePayloadFactory
    {
        public const string FeatureFlagName = "EnvironmentStateRepairJob";

        public const string JobName = "environment_state_repair_job";

        public const string QueueName = EnvironmentJobQueueConstants.EnvironmentStateRepairJob;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentStateRepairJobProducer"/> class.
        /// <param name="jobSchedulerFeatureFlags">Job scheduler feature flags</param>
        /// </summary>
        public EnvironmentStateRepairJobProducer(
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IJobSchedulerFeatureFlags jobSchedulerFeatureFlags)
        {
            CloudEnvironmentRepository = Requires.NotNull(cloudEnvironmentRepository, nameof(cloudEnvironmentRepository));
            JobSchedulerFeatureFlags = jobSchedulerFeatureFlags;
        }

        private ICloudEnvironmentRepository CloudEnvironmentRepository { get; }

        private IJobSchedulerFeatureFlags JobSchedulerFeatureFlags { get; }

        // Run once a day
        private (string CronExpression, TimeSpan Interval) ScheduleTimeInterval => ("0 0 * * *", TimeSpan.FromDays(1));

        public async Task CreatePayloadsAsync(string jobRunId, DateTime scheduleRun, IServiceProvider serviceProvider, OnPayloadsCreatedDelegateAsync onCreated, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            await logger.OperationScopeAsync(
                $"{JobName}_produce_payload",
                async (innerLogger) =>
                {
                    var lastUpdatedDate = DateTime.UtcNow.AddDays(-1);
                    var cloudEnvironmentsToRepair = await CloudEnvironmentRepository.GetEnvironmentsNeedRepairAsync(lastUpdatedDate, innerLogger);

                    logger.FluentAddValue("EnvironmentNeedRepairFound", cloudEnvironmentsToRepair.Count());

                    await onCreated.AddAllPayloadsAsync(cloudEnvironmentsToRepair, (environment) =>
                    {
                        var jobPayload = new EnvironmentStateRepairPayload();
                        jobPayload.EnvironmentId = environment.Id;
                        jobPayload.CurrentState = environment.State;
                        return jobPayload;
                    });
                });
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

        /// <summary>
        /// A environment state repair payload
        /// </summary>
        public class EnvironmentStateRepairPayload : JobPayload
        {
            /// <summary>
            /// Gets or sets current environment state.
            /// </summary>
            public CloudEnvironmentState CurrentState { get; set; }

            /// <summary>
            /// Gets or sets the reference id.
            /// </summary>
            public string EnvironmentId { get; set; }
        }
    }
}
