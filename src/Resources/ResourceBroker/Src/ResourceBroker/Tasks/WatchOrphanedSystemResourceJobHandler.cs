// <copyright file="WatchOrphanedSystemResourceJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts.Constants;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts.Payloads;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Producers;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Watches for Orphaned System Resources. Potential orphans are set to the <see cref="ConfirmOrphanedSystemResourceJobHandler"/>
    /// FrontEnd handler and responses are received by <see cref="ConfirmedOrphanedSystemResourceJobHandler"/>.
    /// </summary>
    /// <remarks>
    /// When making changes in this class take a look at \src\Codespaces\EnvironmentManager\Src\EnvironmentManager\Tasks\WatchOrphanedSystemEnvironmentsTask.cs.
    /// </remarks>
    public class WatchOrphanedSystemResourceJobHandler : JobHandlerPayloadBase<GuidShardJobProducer.GuidShardPayload<WatchOrphanedSystemResourceJobHandler>>, IGuidShardJobScheduleDetails
    {
        public string EnabledFeatureFlagName => "WatchOrphanedSystemResourceJobs";

        public string JobName => "watch_orphaned_system_resource_task";

        public string QueueId => ResourceJobQueueConstants.GenericQueueName;

        // Runs every 2 hours for up to 20 min
        public (string CronExpression, TimeSpan Interval) ScheduleTimeInterval => JobPayloadRegisterSchedule.WatchOrphanedSystemResourceJobSchedule;

        public Type PayloadTagType => typeof(WatchOrphanedSystemResourceJobHandler);

        // Add an artificial delay between DB queries so that we reduce bursty load on our database to prevent throttling for end users
        private static readonly TimeSpan QueryDelay = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchOrphanedSystemResourceJobHandler"/> class.
        /// </summary>
        /// <param name="resourceRepository">Resource repository.</param>
        /// <param name="resourcePoolDefinitionStore">Resource pool definition store.</param>
        /// <param name="queueProducerFactory">Job queue producer factory.</param>
        public WatchOrphanedSystemResourceJobHandler(
            IResourceRepository resourceRepository,
            IResourcePoolDefinitionStore resourcePoolDefinitionStore,
            IJobQueueProducerFactory queueProducerFactory)
        {
            ResourceRepository = resourceRepository;
            ResourcePoolDefinitionStore = resourcePoolDefinitionStore;
            QueueProducer = queueProducerFactory.GetOrCreate(JobQueueIds.ConfirmOrphanedSystemResourceJob);
        }

        private string LogBaseName => ResourceLoggingConstants.WatchOrphanedSystemResourceTask;

        private IResourceRepository ResourceRepository { get; }

        private IResourcePoolDefinitionStore ResourcePoolDefinitionStore { get; }

        private IJobQueueProducer QueueProducer { get; }

        /// <inheritdoc />
        protected override Task HandleJobAsync(GuidShardJobProducer.GuidShardPayload<WatchOrphanedSystemResourceJobHandler> payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            var idShard = payload.Shard;

            logger.FluentAddBaseValue("TaskResourceIdShard", idShard);

            return logger.OperationScopeAsync(
                $"{LogBaseName}_process_shard",
                (loopLogger) =>
                {
                    return ResourceRepository.ForEachAsync(
                        x => x.Id.StartsWith(idShard) &&
                            x.IsAssigned &&
                            !x.IsDeleted &&
                            x.Type != ResourceType.PoolQueue, // PoolQueues are BE only resources and are handled in WatchOrphanedPoolTask
                        loopLogger.NewChildLogger(),
                        (resource, resourceLogger) =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            return HandlePotentiallyOrphanedResourceAsync(resource, resourceLogger, cancellationToken);
                        },
                        (_, __) => Task.Delay(QueryDelay));
                });
        }

        private Task HandlePotentiallyOrphanedResourceAsync(ResourceRecord resource, IDiagnosticsLogger logger, CancellationToken ct)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_process_record",
                async (childLogger) =>
                {
                    // Take care of logging
                    childLogger
                        .FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, resource.Id)
                        .FluentAddValue("ResourceIsAssigned", resource.IsAssigned)
                        .FluentAddValue("ResourceAssigned", resource.Assigned)
                        .FluentAddValue("ResourceEnvironmentAliveDate", resource.KeepAlives?.EnvironmentAlive)
                        .FluentAddValue("ResourceAzureResourceAliveDate", resource.KeepAlives?.AzureResourceAlive)
                        .FluentAddValue("ResourceCreatedDate", resource.Created)
                        .FluentAddValue("ResourceType", resource.Type)
                        .FluentAddValue(ResourceLoggingPropertyConstants.PoolResourceType, resource.Type)
                        .FluentAddValue("ResourceProvisioningStatus", resource.ProvisioningStatus)
                        .FluentAddValue("ResourceProvisioningReason", resource.ProvisioningReason)
                        .FluentAddValue("ResourceStartingStatus", resource.StartingStatus)
                        .FluentAddValue("ResourceStartingReason", resource.StartingReason)
                        .FluentAddValue("ResourceDeletingStatus", resource.DeletingStatus)
                        .FluentAddValue("ResourceDeletingReason", resource.DeletingReason)
                        .FluentAddValue("ResourceCleanupStatus", resource.CleanupStatus)
                        .FluentAddValue("ResourceCleanupReason", resource.CleanupReason);

                    var poolReferenceCode = resource.PoolReference?.Code;

                    var poolDefinition = poolReferenceCode != null
                        ? await ResourcePoolDefinitionStore.MapPoolCodeToResourceSku(resource.PoolReference.Code)
                        : null;

                    if (poolDefinition != null)
                    {
                        childLogger.FluentAddValue(ResourceLoggingPropertyConstants.PoolSkuName, poolDefinition.Details.SkuName);
                    }

                    var correlationId = Guid.NewGuid();
                    childLogger.FluentAddBaseValue(ResourceLoggingConstants.RequestCorrelationId, correlationId);

                    var payload = CreateRequestPayload(resource, correlationId);
                    await QueueProducer.AddJobAsync(payload, default, childLogger.NewChildLogger(), ct);

                    // Pause to rate limit ourselves
                    await Task.Delay(QueryDelay);
                },
                swallowException: true);
        }

        private static JobPayload CreateRequestPayload(ResourceRecord resource, Guid correlationId)
        {
            return new ConfirmOrphanedSystemResourcePayload
            {
                ResourceId = resource.Id,
                ResourceType = resource.Type,
                LoggerProperties = new Dictionary<string, object>
                {
                    [ResourceLoggingConstants.RequestCorrelationId] = correlationId,
                },
            };
        }
    }
}
