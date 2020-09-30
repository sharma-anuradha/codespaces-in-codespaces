// <copyright file="WatchOrphanedComputeImagesJobProducer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// A class that implements IJobSchedulePayloadFactory and able to produce job payloads
    /// for watching orphaned compute images.
    /// </summary>
    public class WatchOrphanedComputeImagesJobProducer : IJobSchedulePayloadFactory, IJobSchedulerRegister
    {
        public const string FeatureFlagName = "WatchOrphanedComputeImagesJob";

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchOrphanedComputeImagesJobProducer"/> class.
        /// <param name="jobSchedulerFeatureFlags">Job scheduler feature flags</param>
        /// </summary>
        public WatchOrphanedComputeImagesJobProducer(
            IJobSchedulerFeatureFlags jobSchedulerFeatureFlags)
        {
            JobSchedulerFeatureFlags = jobSchedulerFeatureFlags;
        }

        private string JobName => "watch_orphaned_compute_image_task";

        private IJobSchedulerFeatureFlags JobSchedulerFeatureFlags { get; }

        // Run once a day
        private (string CronExpression, TimeSpan Interval) ScheduleTimeInterval => JobPayloadRegisterSchedule.WatchOrphanedComputeImagesJobSchedule;

        /// <inheritdoc/>
        public async Task CreatePayloadsAsync(string jobRunId, DateTime scheduleRun, IServiceProvider serviceProvider, OnPayloadCreatedDelegate onCreated, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            await logger.OperationScopeAsync(
                $"{JobName}_produce_payload",
                async (innerLogger) =>
                {
                    // Fetch target images/blobs
                    var artifacts = GetArtifactTypesToCleanup();

                    await onCreated.AddAllPayloadsAsync(artifacts, (artifact) =>
                    {
                        var jobPayload = new WatchOrphanedComputeImagesPayload();
                        jobPayload.ArtifactFamilyType = artifact;
                        return jobPayload;
                    });
                });
        }

        /// <inheritdoc/>
        public void RegisterScheduleJob()
        {
            JobSchedulerFeatureFlags.AddRecurringJobPayload(
                ScheduleTimeInterval.CronExpression,
                jobName: $"{JobName}_run",
                ResourceJobQueueConstants.GenericQueueName,
                claimSpan: ScheduleTimeInterval.Interval,
                this,
                FeatureFlagName);
        }

        private IEnumerable<ImageFamilyType> GetArtifactTypesToCleanup()
        {
            return new List<ImageFamilyType> { ImageFamilyType.Compute, };
        }

        /// <summary>
        /// A watch orphaned compute images payload
        /// </summary>
        public class WatchOrphanedComputeImagesPayload : JobPayload
        {
            public ImageFamilyType ArtifactFamilyType { get; set; }
        }
    }
}
