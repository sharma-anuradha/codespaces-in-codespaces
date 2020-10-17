// <copyright file="GuidShardJobProducer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Producers
{
    /// <summary>
    /// Produces scheduled job payloads for workflows consuming resource id shards
    /// </summary>
    public class GuidShardJobProducer : IJobSchedulerRegister
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataPlaneResourceGroupJobProducer"/> class.
        /// </summary>
        /// <param name="jobSchedulerFeatureFlags">The job scheduler feature flags instance.</param>
        public GuidShardJobProducer(
            IJobSchedulerFeatureFlags jobSchedulerFeatureFlags,
            IEnumerable<IGuidShardJobScheduleDetails> handlers)
        {
            JobSchedulerFeatureFlags = jobSchedulerFeatureFlags;
            JobHandlers = handlers;
        }

        private IJobSchedulerFeatureFlags JobSchedulerFeatureFlags { get; }

        private IEnumerable<IGuidShardJobScheduleDetails> JobHandlers { get; }

        /// <inheritdoc/>
        public void RegisterScheduleJob()
        {
            foreach (var handler in JobHandlers)
            {
                JobSchedulerFeatureFlags.AddRecurringJobPayload(
                    handler.ScheduleTimeInterval.CronExpression,
                    $"{handler.JobName}_run",
                    handler.QueueId,
                    claimSpan: handler.ScheduleTimeInterval.Interval,
                    CreateFactoryForConsumer(handler),
                    handler.EnabledFeatureFlagName);
            }
        }

        private async Task CreatePayloadsAsync(IGuidShardJobScheduleDetails handler, OnPayloadsCreatedDelegateAsync onPayloadCreated)
        {
            await onPayloadCreated.AddAllPayloadsAsync(ScheduledTaskHelpers.GetIdShards(), (shard) =>
            {
                var payload = (GuidShardPayloadBase)Activator.CreateInstance(typeof(GuidShardPayload<>).MakeGenericType(handler.PayloadTagType));                
                payload.Shard = shard;
                return payload;
            });
        }

        private IJobSchedulePayloadFactory CreateFactoryForConsumer(IGuidShardJobScheduleDetails handler)
        {
            return JobSchedulerProducerHelpers.CreateJobSchedulePayloadFactory((jobRunId, dt, srvcProvider, onCreated, logger, ct) => this.CreatePayloadsAsync(handler, onCreated));
        }

        public class GuidShardPayload<T> : GuidShardPayloadBase
            where T : class
        {
        }

        public class GuidShardPayloadBase : JobPayload
        {
            /// <summary>
            /// Gets or sets the shard.
            /// </summary>
            public string Shard { get; set; }
        }
    }
}
