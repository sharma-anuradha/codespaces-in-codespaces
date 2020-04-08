// <copyright file="ResourceBroker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Resource broker.
    /// </summary>
    public class ResourceBroker : IResourceBroker
    {
        private const string LogBaseName = ResourceLoggingConstants.ResourceBroker;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceBroker"/> class.
        /// </summary>
        /// <param name="resourceRepository">Resource repository that should be used.</param>
        /// <param name="resourcePool">Resource pool that should be used.</param>
        /// <param name="resourceScalingStore">Target resource scaling store.</param>
        /// <param name="resourceContinuationOperations">Target continuation task sctivator.</param>
        /// <param name="taskHelper">Target task helper.</param>
        /// <param name="mapper">Mapper that should be used.</param>
        public ResourceBroker(
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

        private IResourceRepository ResourceRepository { get; }

        private IResourcePoolManager ResourcePool { get; }

        private IResourcePoolDefinitionStore ResourceScalingStore { get; }

        private IResourceContinuationOperations ResourceContinuationOperations { get; }

        private ITaskHelper TaskHelper { get; }

        private IMapper Mapper { get; }

        /// <inheritdoc/>
        public Task<IEnumerable<AllocateResult>> AllocateAsync(
            Guid environmentId, IEnumerable<AllocateInput> inputs, string trigger, IDiagnosticsLogger logger)
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
                                    assignResult = await TryQueueAsync(input, itemLogger);
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
                                // Trigger auto pool create to replace assigned item
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

                    // Release each of the items that had been assigned so far
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
            Guid environmentId, AllocateInput input, string trigger, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_allocate",
                async (childLogger) =>
                {
                    (ResourceRecord Resource, ResourcePool ResourceSku) assignResult = default;

                    if (input.QueueCreateResource)
                    {
                        // Try to create resource for request.
                        assignResult = await TryQueueAsync(input, childLogger);
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

        /// <inheritdoc/>
        public Task<bool> StartAsync(
            Guid environmentId, StartAction action, IEnumerable<StartInput> resources, string trigger, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_start",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("StartAction", action.ToString());

                    // Match resources to records
                    var backingResources = new List<(StartInput Resource, ResourceRecord Record)>();
                    foreach (var resource in resources)
                    {
                        var record = await ResourceRepository.GetAsync(resource.ResourceId.ToString(), logger.NewChildLogger());
                        backingResources.Add((Resource: resource, Record: record));
                    }

                    // Switch between different actions
                    switch (action)
                    {
                        case StartAction.StartCompute:

                            if (resources.Count() == 2 || resources.Count() == 3)
                            {
                                // Select target resorces
                                var computeResource = backingResources.Single(x => x.Record.Type == ResourceType.ComputeVM);
                                var storageResource = backingResources.Single(x => x.Record.Type == ResourceType.StorageFileShare);
                                var archiveStorageResource = backingResources.SingleOrDefault(x => x.Record.Type == ResourceType.StorageArchive);

                                childLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, computeResource.Resource.ResourceId)
                                    .FluentAddBaseValue("StorageResourceId", storageResource.Resource?.ResourceId)
                                    .FluentAddBaseValue("ArchiveStorageResourceId", archiveStorageResource.Resource?.ResourceId);

                                // Trigger environment start
                                await ResourceContinuationOperations.StartEnvironmentAsync(
                                    environmentId,
                                    computeResource.Resource.ResourceId,
                                    storageResource.Resource.ResourceId,
                                    archiveStorageResource.Resource?.ResourceId,
                                    computeResource.Resource.Variables,
                                    trigger,
                                    childLogger.NewChildLogger());
                            }
                            else
                            {
                                throw new NotSupportedException($"Start compute action expects 2 resource and {resources.Count()} was supplied.");
                            }

                            break;
                        case StartAction.StartArchive:
                            if (resources.Count() == 2)
                            {
                                // Select target resorces
                                var blobResource = backingResources.Single(x => x.Record.Type == ResourceType.StorageArchive);
                                var storageResource = backingResources.Single(x => x.Record.Type == ResourceType.StorageFileShare);

                                childLogger.FluentAddBaseValue("StorageResourceId", storageResource.Resource.ResourceId)
                                    .FluentAddBaseValue("ArchiveStorageResourceId", blobResource.Resource.ResourceId);

                                await ResourceContinuationOperations.StartArchiveAsync(
                                    environmentId,
                                    blobResource.Resource.ResourceId,
                                    storageResource.Resource.ResourceId,
                                    trigger,
                                    childLogger.NewChildLogger());
                            }
                            else
                            {
                                throw new NotSupportedException($"Archive stroage action expects 2 resource and {resources.Count()} was supplied.");
                            }

                            break;
                    }

                    return true;
                });
        }

        /// <inheritdoc/>
        public Task<bool> StartAsync(
            Guid environmentId, StartAction action, StartInput input, string trigger, IDiagnosticsLogger logger)
        {
            throw new NotSupportedException("No action type supports the starting of a single resource.");
        }

        /// <inheritdoc/>
        public Task<bool> SuspendAsync(
            Guid environmentId, IEnumerable<SuspendInput> inputs, string trigger, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_suspend_set",
                async (childLogger) =>
                {
                    var results = await Task.WhenAll(
                        inputs.Select(input => SuspendAsync(environmentId, input, trigger, childLogger.NewChildLogger())));

                    return results.All(x => x);
                });
        }

        /// <inheritdoc/>
        public Task<bool> SuspendAsync(
            Guid environmentId, SuspendInput input, string trigger, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_suspend",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, input.ResourceId);

                    await ResourceContinuationOperations.SuspendAsync(
                        environmentId, input.ResourceId, trigger, logger);

                    return true;
                });
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(
            Guid environmentId, IEnumerable<DeleteInput> inputs, string trigger, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_delete_set",
                async (childLogger) =>
                {
                    var results = await Task.WhenAll(
                        inputs.Select(input => DeleteAsync(environmentId, input, trigger, childLogger.NewChildLogger())));

                    return results.All(x => x);
                });
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(
            Guid environmentId, DeleteInput input, string trigger, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_delete",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, input.ResourceId);

                    await ResourceContinuationOperations.DeleteAsync(
                        environmentId, input.ResourceId, trigger, childLogger.NewChildLogger());

                    return true;
                });
        }

        /// <inheritdoc/>
        public Task<IEnumerable<StatusResult>> StatusAsync(
            Guid environmentId, IEnumerable<StatusInput> inputs, string trigger, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_status_set",
                async (childLogger) =>
                {
                    var results = await Task.WhenAll(
                        inputs.Select(input => StatusAsync(environmentId, input, trigger, childLogger.NewChildLogger())));

                    return results.AsEnumerable();
                });
        }

        /// <inheritdoc/>
        public Task<StatusResult> StatusAsync(
            Guid environmentId, StatusInput input, string trigger, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_status",
                async (childLogger) =>
                {
                    // Get record from db
                    var record = await ResourceRepository.GetAsync(input.ResourceId.ToString(), logger.NewChildLogger());
                    var recordDetails = record.GetDetails();

                    // Build result
                    var result = new StatusResult()
                    {
                        ResourceId = Guid.Parse(record.Id),
                        SkuName = recordDetails.SkuName,
                        Location = recordDetails.Location,
                        Type = record.Type,
                        IsReady = record.IsReady,
                        ProvisioningStatus = record.ProvisioningStatus,
                        ProvisioningStatusChanged = record.ProvisioningStatusChanged,
                        StartingStatus = record.StartingStatus,
                        StartingStatusChanged = record.StartingStatusChanged,
                        DeletingStatus = record.DeletingStatus,
                        DeletingStatusChanged = record.DeletingStatusChanged,
                        CleanupStatus = record.CleanupStatus,
                        CleanupStatusChanged = record.CleanupStatusChanged,
                    };

                    return result;
                });
        }

        /// <inheritdoc/>
        public Task<bool> ProcessHeartbeatAsync(Guid id, string trigger, IDiagnosticsLogger logger)
        {
            return logger.RetryOperationScopeAsync(
                $"{LogBaseName}_processheartbeat",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("ResourceId", id);

                    var exists = false;

                    // Try to get result
                    var result = await ResourceRepository.GetAsync(id.ToString(), childLogger.NewChildLogger());
                    if (result != null)
                    {
                        // Set keep alives
                        result.KeepAlives.EnvironmentAlive = DateTime.UtcNow;

                        // Update record
                        await ResourceRepository.UpdateAsync(result, childLogger.NewChildLogger());

                        // If null or is deleted then it doesn't exist
                        exists = result?.IsDeleted == false;
                    }

                    childLogger.FluentAddValue("ResourceExists", exists)
                        .FluentAddValue("ResourceIsNotNull", result != null)
                        .FluentAddValue("ResourceIsDeleted", result != null ? (bool?)result.IsDeleted : null);

                    return exists;
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
                var resourceSku = await MapLogicalSkuToResourceSku(input.SkuName, input.Type, input.Location);

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
            IDiagnosticsLogger logger)
        {
            // Map logical sku to resource sku
            var resourceSku = await MapLogicalSkuToResourceSku(input.SkuName, input.Type, input.Location);

            var resourceId = Guid.NewGuid();
            var resource = await ResourceContinuationOperations.QueueCreateAsync(
            resourceId, input.Type, resourceSku.Details, "ResourceQueueAllocate", logger.NewChildLogger());

            return (Resource: resource, ResourceSku: resourceSku);
        }

        private async Task<ResourcePool> MapLogicalSkuToResourceSku(string skuName, ResourceType type, AzureLocation location)
        {
            var resources = await ResourceScalingStore.RetrieveDefinitions();

            var resourceSku = resources
                .Where(x => x.Details.Location == location
                    && x.Type == type
                    && x.EnvironmentSkus.Contains(skuName));

            if (!resourceSku.Any())
            {
                throw new ArgumentException($"Sku resource match was not found. SkuName = {skuName}, Type = {type}, Location = {location}");
            }

            if (resourceSku.Count() > 1)
            {
                throw new ArgumentException($"More than one Sku resource match was found. SkuName = {skuName}, Type = {type}, Location = {location}");
            }

            return resourceSku.Single();
        }

        private async Task MapSourceComputeOS(AllocateInput input, ResourceRecord resource)
        {
            if (input.Type == ResourceType.StorageArchive || input.Type == ResourceType.StorageFileShare)
            {
                // Capture source vm type
                var resourceSku = await MapLogicalSkuToResourceSku(input.SkuName, ResourceType.ComputeVM, input.Location);
                var targetOS = ((ResourcePoolComputeDetails)resourceSku.Details).OS;

                // Persist target os type
                var archiveShareResource = resource.GetStorageDetails();
                archiveShareResource.SourceComputeOS = targetOS;
            }
        }
    }
}