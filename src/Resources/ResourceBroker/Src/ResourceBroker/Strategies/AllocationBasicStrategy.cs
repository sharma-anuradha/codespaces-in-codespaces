// <copyright file="AllocationBasicStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Strategies
{
    /// <summary>
    /// Allocation basic strategy.
    /// </summary>
    public class AllocationBasicStrategy : IAllocationStrategy
    {
        private const string LogBaseName = ResourceLoggingConstants.ResourceBrokerAllocatorBasic;

        private static readonly ResourceType[] SupportedTypes = new ResourceType[]
        {
            ResourceType.ComputeVM,
            ResourceType.KeyVault,
            ResourceType.StorageArchive,
            ResourceType.StorageFileShare,
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="AllocationBasicStrategy"/> class.
        /// </summary>
        /// <param name="resourceRepository">Resource repository.</param>
        /// <param name="resourcePool">Resource pool.</param>
        /// <param name="resourceScalingStore">resource scaling store.</param>
        /// <param name="resourceContinuationOperations">Resource continuation operations.</param>
        /// <param name="taskHelper">Task helper.</param>
        /// <param name="mapper">Mapper.</param>
        public AllocationBasicStrategy(
            IResourceRepository resourceRepository,
            IResourcePoolManager resourcePool,
            IResourcePoolDefinitionStore resourceScalingStore,
            IResourceContinuationOperations resourceContinuationOperations,
            ITaskHelper taskHelper,
            IMapper mapper)
        {
            ResourceRepository = Requires.NotNull(resourceRepository, nameof(resourceRepository));
            ResourcePool = Requires.NotNull(resourcePool, nameof(resourcePool));
            ResourceScalingStore = Requires.NotNull(resourceScalingStore, nameof(resourceScalingStore));
            ResourceContinuationOperations = Requires.NotNull(resourceContinuationOperations, nameof(resourceContinuationOperations));
            TaskHelper = Requires.NotNull(taskHelper, nameof(taskHelper));
            Mapper = Requires.NotNull(mapper, nameof(mapper));
        }

        /// <summary>
        /// Gets resource repository.
        /// </summary>
        private IResourceRepository ResourceRepository { get; }

        /// <summary>
        /// Gets resource pool.
        /// </summary>
        private IResourcePoolManager ResourcePool { get; }

        /// <summary>
        /// Gets resource scaling store.
        /// </summary>
        private IResourcePoolDefinitionStore ResourceScalingStore { get; }

        /// <summary>
        /// Gets resource continuation operations.
        /// </summary>
        private IResourceContinuationOperations ResourceContinuationOperations { get; }

        /// <summary>
        /// Gets task helper.
        /// </summary>
        private ITaskHelper TaskHelper { get; }

        /// <summary>
        /// Gets mapper.
        /// </summary>
        private IMapper Mapper { get; }

        /// <inheritdoc/>
        public bool CanHandle(IEnumerable<AllocateInput> inputs)
        {
            return inputs.All(x => SupportedTypes.Contains(x.Type));
        }

        /// <inheritdoc/>
        public Task<IEnumerable<AllocateResult>> AllocateAsync(
            Guid environmentId, IEnumerable<AllocateInput> inputs, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_allocate_set",
                async (childLogger) =>
                {
                    var failedInput = default(AllocateInput);
                    var resourceResults = new List<(AllocateInput Input, ResourceRecord Record, ResourcePool ResourceSku)>();

                    // Work through each input
                    foreach (var input in inputs)
                    {
                        (AllocateInput Input, ResourceRecord Resource, ResourcePool ResourceSku) resourceResult = default;

                        // Setting up logger
                        logger.FluentAddBaseValue("ResourceLocation", input.Location.ToString())
                            .FluentAddBaseValue("ResourceSystemSkuName", input.SkuName)
                            .FluentAddBaseValue("ResourceType", input.Type.ToString())
                            .FluentAddBaseValue("ResourceQueueAllocation", input.QueueCreateResource);

                        // Try and get item from the pool
                        resourceResult = await childLogger.OperationScopeAsync(
                            $"{LogBaseName}_allocate",
                            async (itemLogger) =>
                            {
                                (ResourceRecord Resource, ResourcePool ResourceSku) assignResult = default;

                                if (input.QueueCreateResource)
                                {
                                    // Try to create resource for request.
                                    assignResult = await TryQueueAsync(input, itemLogger, loggingProperties);
                                }
                                else
                                {
                                    // Try and get item from the pool
                                    assignResult = await TryGetAsync(input, itemLogger);
                                }

                                // Deal with case that it didn't exist
                                if (assignResult.Resource == null)
                                {
                                    failedInput = input;
                                }
                                else
                                {
                                    itemLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, assignResult.Resource.Id);
                                }

                                return (Input: input, Resource: assignResult.Resource, ResourceSku: assignResult.ResourceSku);
                            });

                        // Abort things at the first fail we hit
                        if (resourceResult.Resource == null)
                        {
                            break;
                        }

                        // Add to result set
                        resourceResults.Add(resourceResult);
                    }

                    // Valdiate that things worked as expected
                    var isValid = resourceResults.Count == inputs.Count() && failedInput == null;

                    childLogger.FluentAddValue("AllocationIsValid", isValid)
                               .FluentAddValue("AllocationAllocationCount", resourceResults.Count);

                    if (isValid)
                    {
                        var results = new List<AllocateResult>();

                        // Map results and trigger create to replace allocated items if needed
                        foreach (var resourceResult in resourceResults)
                        {
                            var resourceSku = resourceResult.ResourceSku;
                            var record = resourceResult.Record;

                            // Only trigger pool refresh if its a pool resource
                            if (resourceSku != null && !resourceResult.Input.QueueCreateResource)
                            {
                                // Trigger auto pool create to replace assigned item. Pass any logging property that we want to log. Nothing as of now.
                                TaskHelper.RunBackground(
                                    $"{LogBaseName}_run_create",
                                    (taskLogger) => ResourceContinuationOperations.CreateAsync(
                                        Guid.NewGuid(), resourceSku.Type, resourceSku.Details, "ResourceAssignedReplace", taskLogger),
                                    childLogger);
                            }

                            results.Add(Mapper.Map<AllocateResult>(record));
                        }

                        return (IEnumerable<AllocateResult>)results;
                    }

                    // Release each of the items that had been assigned so far (if we couldn't have required number of resources i.e. isValid == false.
                    foreach (var resourceResult in resourceResults)
                    {
                        var input = resourceResult.Input;
                        var record = resourceResult.Record;

                        // Try and get item from the pool
                        await childLogger.OperationScopeAsync(
                                $"{LogBaseName}_release",
                                async (itemLogger) =>
                                {
                                    itemLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, record.Id)
                                        .FluentAddBaseValue("ResourceLocation", record.Location)
                                        .FluentAddBaseValue("ResourceSystemSkuName", input.SkuName)
                                        .FluentAddBaseValue("ResourceType", record.Type);

                                    await ResourcePool.ReleaseGetAsync(
                                        record.Id,
                                        itemLogger.NewChildLogger());
                                },
                                swallowException: true);
                    }

                    // Throw exception since we failed to allocate
                    throw new OutOfCapacityException(failedInput.SkuName, failedInput.Type, failedInput.Location.ToString().ToLowerInvariant());
                });
        }

        /// <inheritdoc/>
        public Task<AllocateResult> AllocateAsync(
            Guid environmentId, AllocateInput input, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_allocate",
                async (childLogger) =>
                {
                    (ResourceRecord Resource, ResourcePool ResourceSku) assignResult = default;

                    if (input.QueueCreateResource)
                    {
                        // Try to create resource for request.
                        assignResult = await TryQueueAsync(input, childLogger, loggingProperties);
                    }
                    else
                    {
                        // Try and get item from the pool
                        assignResult = await TryGetAsync(input, childLogger);
                    }

                    // Deal with case that it didn't exist
                    if (assignResult.Resource == null)
                    {
                        throw new OutOfCapacityException(input.SkuName, input.Type, input.Location.ToString().ToLowerInvariant());
                    }

                    childLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, assignResult.Resource.Id);

                    // Only trigger pool refresh if its a pool resource
                    if (assignResult.ResourceSku != null)
                    {
                        // Trigger auto pool create to replace assigned item
                        TaskHelper.RunBackground(
                            $"{LogBaseName}_run_create",
                            (taskLogger) => ResourceContinuationOperations.CreateAsync(
                                Guid.NewGuid(), assignResult.ResourceSku.Type, assignResult.ResourceSku.Details, "ResourceAssignedReplace", taskLogger),
                            childLogger);
                    }

                    return Mapper.Map<AllocateResult>(assignResult.Resource);
                });
        }

        private async Task<(ResourceRecord Resource, ResourcePool ResourceSku)> TryGetAsync(AllocateInput input, IDiagnosticsLogger itemLogger)
        {
            // If a blob is being created, we don't need to go to the pool for that
            if (input.Type == ResourceType.StorageArchive)
            {
                // Common properties
                var id = Guid.NewGuid();
                var time = DateTime.UtcNow;
                var type = input.Type;
                var location = input.Location;
                var skuName = "ShrunkBlob";

                // Core recrod
                var resource = ResourceRecord.Build(id, time, type, location, skuName);
                resource.IsAssigned = true;
                resource.Assigned = time;
                resource.IsReady = true;
                resource.Ready = time;

                // Copy across extended details
                await MapSourceComputeOS(input, resource);

                // Create the actual record
                resource = await ResourceRepository.CreateAsync(resource, itemLogger.NewChildLogger());

                return (resource, null);
            }
            else
            {
                // Map logical sku to resource sku
                var resourceSku = await ResourceScalingStore.MapLogicalSkuToResourceSku(input.SkuName, input.Type, input.Location);

                itemLogger.FluentAddBaseValue("ResourceResourceSkuName", resourceSku.Details.SkuName);

                // Try and get item from the pool
                var resource = await ResourcePool.TryGetAsync(
                    resourceSku.Details.GetPoolDefinition(), itemLogger.NewChildLogger());

                itemLogger.FluentAddBaseValue("ResourceAllocateFound", resource != null);

                return (resource, resourceSku);
            }
        }

        private async Task<(ResourceRecord Resource, ResourcePool ResourceSku)> TryQueueAsync(
            AllocateInput input,
            IDiagnosticsLogger logger,
            IDictionary<string, string> loggingProperties = null)
        {
            // Map logical sku to resource sku
            var resourceSku = await ResourceScalingStore.MapLogicalSkuToResourceSku(input.SkuName, input.Type, input.Location);

            var resourceId = Guid.NewGuid();
            var resource = await ResourceContinuationOperations.QueueCreateAsync(
            resourceId, input.Type, input.ExtendedProperties, resourceSku.Details, "ResourceQueueAllocate", logger.NewChildLogger(), loggingProperties);

            return (Resource: resource, ResourceSku: resourceSku);
        }

        private async Task MapSourceComputeOS(AllocateInput input, ResourceRecord resource)
        {
            if (input.Type == ResourceType.StorageArchive || input.Type == ResourceType.StorageFileShare)
            {
                // Capture source vm type
                var resourceSku = await ResourceScalingStore.MapLogicalSkuToResourceSku(input.SkuName, ResourceType.ComputeVM, input.Location);
                var targetOS = ((ResourcePoolComputeDetails)resourceSku.Details).OS;

                // Persist target os type
                var archiveShareResource = resource.GetStorageDetails();
                archiveShareResource.SourceComputeOS = targetOS;
            }
        }
    }
}
