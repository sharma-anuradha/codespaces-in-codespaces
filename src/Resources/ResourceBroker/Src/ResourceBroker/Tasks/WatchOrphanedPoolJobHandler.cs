// <copyright file="WatchOrphanedPoolJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Handler that tries to kick off a continuation which will try and manage tracking
    /// orphaned pools and conduct orchestrate drains as requried.
    /// </summary>
    public class WatchOrphanedPoolJobHandler : JobHandlerPayloadBase<WatchOrphanedPoolJobProducer.WatchOrphanedPoolPayload>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchOrphanedPoolJobHandler"/> class.
        /// <param name="resourceRepository">Resource Repository.</param>
        /// <param name="resourceContinuationOperations">ResourceContinuationOperations object to perform the necessary workflows.</param>
        /// </summary>
        public WatchOrphanedPoolJobHandler(
            IResourceRepository resourceRepository,
            IResourceContinuationOperations resourceContinuationOperations)
        {
            ResourceRepository = resourceRepository;
            ResourceContinuationOperations = Requires.NotNull(resourceContinuationOperations, nameof(resourceContinuationOperations));
        }

        private string LogBaseName => ResourceLoggingConstants.WatchOrphanedPoolJobHandler;

        private IResourceRepository ResourceRepository { get; }

        private IResourceContinuationOperations ResourceContinuationOperations { get; }

        /// <inheritdoc/>
        protected override async Task HandleJobAsync(WatchOrphanedPoolJobProducer.WatchOrphanedPoolPayload payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            // Queries for resources that
            // 1. belongs to resourcePool and exists and unAssigned.
            // 2. represents poolQueue for this resource pool.
            await ResourceRepository.ForEachAsync(
                x => (x.PoolReference.Code == payload.PoolReferenceCode &&
                    x.IsAssigned == false &&
                    x.IsDeleted == false) ||
                    x.PoolReference.Code == payload.PoolReferenceCode.GetPoolQueueDefinition(),
                logger.NewChildLogger(),
                (resource, innerLogger) =>
                {
                    innerLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, resource.Id);

                    // Log each item
                    return innerLogger.OperationScopeAsync(
                            $"{LogBaseName}_process_record",
                            async (childLogger) =>
                            {
                                await DeleteResourceAsync(resource.Id, childLogger);
                            });
                });
        }

        /// <summary>
        /// Deletes the resource with ResourceContinuationOperation,
        /// just marked as delete untill the dependent resources are deleted in Azure.
        /// </summary>
        /// <param name="id">Resource Id.</param>
        /// <param name="logger">Logger to be used.</param>
        /// <returns>A ResourceRecord Id that got deleted.<see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<string> DeleteResourceAsync(string id, IDiagnosticsLogger logger)
        {
            var resourceRecord = await ResourceRepository.GetAsync(id, logger);
            var isAssigned = resourceRecord?.IsAssigned == true;
            var isDeleted = resourceRecord?.IsDeleted == true;
            var isPoolQueue = resourceRecord?.Type == ResourceType.PoolQueue;

            // Delete if resource exists and not assigned or if it is a pool queue resource
            var shouldDelete = (!isAssigned && !isDeleted) || isPoolQueue;
            string deletedResourceId = null;

            logger
                .FluentAddBaseValue("PoolIsAssigned", isAssigned)
                .FluentAddBaseValue("PoolIsDeleted", isDeleted)
                .FluentAddBaseValue("PoolQueue", isPoolQueue)
                .FluentAddBaseValue("PoolShouldDelete", shouldDelete);

            // Double checking to make sure for deletion.
            if (shouldDelete)
            {
                await logger.OperationScopeAsync(
                    $"{LogBaseName}_delete_record",
                    async (innerLogger) =>
                    {
                        var reason = "OrphanedPoolResource";
                        innerLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.OperationReason, reason);

                        // Since we don't have this pool's Skuname anymore needed, we are just going to perform a delete for this record
                        await ResourceContinuationOperations.DeleteAsync(null, new Guid(id), reason, logger.NewChildLogger());

                        deletedResourceId = id;
                    });
            }

            return deletedResourceId;
        }
    }
}
