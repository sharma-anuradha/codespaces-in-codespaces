// <copyright file="WatchOrphanedPoolJobProducer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// A class that implements IJobSchedulePayloadFactory and able to produce job payloads
    /// for watching orphaned pools
    /// </summary>
    public class WatchOrphanedPoolJobProducer : IJobSchedulePayloadFactory, IJobSchedulerRegister
    {
        public const string FeatureFlagName = "WatchOrphanedPoolJob";

        private string jobName = "watch_orphaned_pool_job";

        // Run once a day
        private (string CronExpression, TimeSpan Interval) ScheduleTimeInterval => JobPayloadRegisterSchedule.WatchOrphanedPoolJobSchedule;

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchOrphanedPoolJobProducer"/> class.
        /// </summary>
        /// <param name="resourceRepository">Target resource repository.</param>
        /// <param name="resourcePoolDefinitionStore">ResourcePoolDefinitionStore info.</param>
        public WatchOrphanedPoolJobProducer(
            IResourceRepository resourceRepository,
            IResourcePoolDefinitionStore resourcePoolDefinitionStore,
            IJobSchedulerFeatureFlags jobSchedulerFeatureFlags)
        {
            ResourcePoolDefinitionStore = Requires.NotNull(resourcePoolDefinitionStore, nameof(resourcePoolDefinitionStore));
            ResourceRepository = Requires.NotNull(resourceRepository, nameof(resourceRepository));
            JobSchedulerFeatureFlags = jobSchedulerFeatureFlags;
        }

        private IResourcePoolDefinitionStore ResourcePoolDefinitionStore { get; }

        private IResourceRepository ResourceRepository { get; }

        private IJobSchedulerFeatureFlags JobSchedulerFeatureFlags { get; }

        /// <inheritdoc/>
        public async Task CreatePayloadsAsync(string jobRunId, DateTime scheduleRun, IServiceProvider serviceProvider, OnPayloadCreatedDelegate onPayloadCreated, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            var allPoolcodes = await FetchPoolCodesAsync(logger);
            var resourcePools = await ResourcePoolDefinitionStore.RetrieveDefinitionsAsync();

            var nonActivePools = new HashSet<string>();

            if (resourcePools.Any())
            {
                foreach (var poolCode in allPoolcodes)
                {
                    var isActive = IsActivePool(poolCode, resourcePools);

                    if (!isActive)
                    {
                        nonActivePools.Add(poolCode);
                    }
                }

                if (nonActivePools.Any())
                {
                    await logger.OperationScopeAsync(
                        $"{jobName}_produce_payload",
                        async (innerLogger) =>
                        {
                            // for each pool in non-active pools, produce a payload for deletion process
                            await onPayloadCreated.AddAllPayloadsAsync(nonActivePools, (poolCode) =>
                            {
                                innerLogger.FluentAddBaseValue("TaskResourcePoolToBeDeleted", poolCode);

                                var jobPayload = new WatchOrphanedPoolPayload();
                                jobPayload.PoolReferenceCode = poolCode;
                                return jobPayload;
                            });
                        });
                }
            }
        }

        /// <inheritdoc/>
        public void RegisterScheduleJob()
        {
            JobSchedulerFeatureFlags.AddRecurringJobPayload(
                ScheduleTimeInterval.CronExpression,
                jobName: $"{jobName}_run",
                ResourceJobQueueConstants.GenericQueueName,
                claimSpan: ScheduleTimeInterval.Interval,
                this,
                FeatureFlagName);
        }

        /// <summary>
        /// Fetch pool codes
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <returns>A list of pool codes</returns>
        private async Task<IEnumerable<string>> FetchPoolCodesAsync(IDiagnosticsLogger logger)
        {
            // Fetch distinct list of pool codes
            var poolCodes = await ResourceRepository.GetPoolCodesForUnassignedAsync(logger);
            var poolQueueCodes = await ResourceRepository.GetAllPoolQueueCodesAsync(logger);

            var allPoolcodes = new HashSet<string>(poolCodes);

            // Add pool codes for pools, that do not contain any unassigned resources, but have pool queues. 
            foreach (var poolQueueCode in poolQueueCodes)
            {
                var poolCode = poolQueueCode.GetPoolCodeForQueue();

                if (!allPoolcodes.Contains(poolCode))
                {
                    allPoolcodes.Add(poolCode);
                }
            }

            return allPoolcodes;
        }

        /// <summary>
        /// Checks whether the given pool is active or not.
        /// </summary>
        /// <param name="poolReferenceCode">PoolReferenceCode.</param>
        /// <param name="resourcePools">List of resource pools that are active.</param>
        /// <returns>A boolean to know if its active.</returns>
        public bool IsActivePool(string poolReferenceCode, IEnumerable<ResourcePool> resourcePools)
        {
            return resourcePools.Any(pool => pool.Id == poolReferenceCode);
        }

        /// <summary>
        /// A watch orphaned pool payload
        /// </summary>
        public class WatchOrphanedPoolPayload : JobPayload
        {
            public string PoolReferenceCode { get; set; }
        }
    }
}
