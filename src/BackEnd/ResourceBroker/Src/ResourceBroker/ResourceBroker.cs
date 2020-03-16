﻿// <copyright file="ResourceBroker.cs" company="Microsoft">
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
            IEnumerable<AllocateInput> inputs, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_allocate_set",
                async (childLogger) =>
                {
                    var failedInput = default(AllocateInput);
                    var resourceResults = new List<(AllocateInput Input, ResourceRecord Record, ResourcePool Pool)>();

                    // Work through each input
                    foreach (var input in inputs)
                    {
                        // Try and get item from the pool
                        var resourceResult = await childLogger.OperationScopeAsync(
                            $"{LogBaseName}_allocate",
                            async (itemLogger) =>
                            {
                                // Try and get item from the pool
                                var assignResult = await TryGetAsync(input, itemLogger);

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

                        // Trigger create to replace allocated items and map results
                        foreach (var resourceResult in resourceResults)
                        {
                            var pool = resourceResult.Pool;
                            var record = resourceResult.Record;

                            // Trigger auto pool create to replace assigned item
                            TaskHelper.RunBackground(
                                $"{LogBaseName}_run_create",
                                async (taskLogger) =>
                                {
                                    var reason = "ResourceAssignedReplace";
                                    var id = Guid.NewGuid();
                                    await ResourceContinuationOperations.CreateAsync(
                                        id, pool.Type, pool.Details, reason, taskLogger);
                                },
                                childLogger);

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
                                    record.Id, itemLogger.NewChildLogger());
                            },
                            swallowException: true);
                    }

                    // Throw exception since we failed to allocate
                    throw new OutOfCapacityException(failedInput.SkuName, failedInput.Type, failedInput.Location.ToString().ToLowerInvariant());
                });
        }

        /// <inheritdoc/>
        public Task<AllocateResult> AllocateAsync(AllocateInput input, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_allocate",
                async (childLogger) =>
                {
                    // Try and get item from the pool
                    var assignResult = await TryGetAsync(input, childLogger);

                    // Deal with case that it didn't exist
                    if (assignResult.Resource == null)
                    {
                        throw new OutOfCapacityException(input.SkuName, input.Type, input.Location.ToString().ToLowerInvariant());
                    }

                    childLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, assignResult.Resource.Id);

                    // Trigger auto pool create to replace assigned item
                    TaskHelper.RunBackground(
                        $"{LogBaseName}_run_create",
                        async (taskLogger) =>
                        {
                            var reason = "ResourceAssignedReplace";
                            var id = Guid.NewGuid();
                            await ResourceContinuationOperations.CreateAsync(
                                id, assignResult.ResourceSku.Type, assignResult.ResourceSku.Details, reason, taskLogger);
                        },
                        childLogger);

                    return Mapper.Map<AllocateResult>(assignResult.Resource);
                });
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(
            IEnumerable<DeleteInput> inputs,
            IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_delete_set",
                async (childLogger) =>
                {
                    var results = await Task.WhenAll(inputs.Select(input => DeleteAsync(input, childLogger.NewChildLogger())));

                    return results.All(x => x);
                });
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(
            DeleteInput input,
            IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_delete",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, input.ResourceId);

                    await ResourceContinuationOperations.DeleteAsync(
                        input.ResourceId, input.Trigger, childLogger.NewChildLogger());

                    return true;
                });
        }

        /// <inheritdoc/>
        public Task<bool> SuspendAsync(
            IEnumerable<SuspendInput> inputs,
            IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_suspend_set",
                async (childLogger) =>
                {
                    var results = await Task.WhenAll(inputs.Select(input => SuspendAsync(input, childLogger.NewChildLogger())));

                    return results.All(x => x);
                });
        }

        /// <inheritdoc/>
        public Task<bool> SuspendAsync(
            SuspendInput input,
            IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_suspend",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, input.ResourceId);

                    await ResourceContinuationOperations.SuspendAsync(input.ResourceId, input.EnvironmentId, input.Trigger, logger);

                    return true;
                });
        }

        /// <inheritdoc/>
        public Task<bool> StartAsync(
            StartInput input,
            IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_start",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, input.ComputeResourceId)
                        .FluentAddBaseValue("StorageResourceId", input.StorageResourceId);

                    await ResourceContinuationOperations.StartAsync(
                        input.ComputeResourceId, input.StorageResourceId, input.EnvironmentVariables, input.Trigger, childLogger.NewChildLogger());

                    return true;
                });
        }

        /// <inheritdoc/>
        public Task<bool> ProcessHeartbeatAsync(Guid id, IDiagnosticsLogger logger)
        {
            return logger.RetryOperationScopeAsync(
                $"{LogBaseName}_exists",
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
            // Setting up logger
            itemLogger.FluentAddBaseValue("ResourceLocation", input.Location.ToString())
                .FluentAddBaseValue("ResourceSystemSkuName", input.SkuName)
                .FluentAddBaseValue("ResourceType", input.Type.ToString());

            // Map logical sku to resource sku
            var resourceSku = await MapLogicalSkuToResourceSku(input.SkuName, input.Type, input.Location);

            itemLogger.FluentAddBaseValue("ResourceResourceSkuName", resourceSku.Details.SkuName);

            // Try and get item from the pool
            var resource = await ResourcePool.TryGetAsync(
                        resourceSku.Details.GetPoolDefinition(), itemLogger.NewChildLogger());

            itemLogger.FluentAddBaseValue("ResourceResourceAllocateFound", resource != null);

            return (resource, resourceSku);
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
    }
}
