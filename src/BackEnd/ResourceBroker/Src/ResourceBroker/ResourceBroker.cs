// <copyright file="ResourceBroker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    ///
    /// </summary>
    public class ResourceBroker : IResourceBroker
    {
        private const string LogBaseName = ResourceLoggingConstants.ResourceBroker;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceBroker"/> class.
        /// </summary>
        /// <param name="resourcePool">Resource pool that should be used.</param>
        /// <param name="resourceScalingStore">Target resource scaling store.</param>
        /// <param name="continuationTaskActivator">Target continuation task sctivator.</param>
        /// <param name="taskHelper">Target task helper.</param>
        /// <param name="mapper">Mapper that should be used.</param>
        public ResourceBroker(
            IResourcePoolManager resourcePool,
            IResourcePoolDefinitionStore resourceScalingStore,
            IContinuationTaskActivator continuationTaskActivator,
            ITaskHelper taskHelper,
            IMapper mapper)
        {
            ResourcePool = Requires.NotNull(resourcePool, nameof(resourcePool));
            ResourceScalingStore = Requires.NotNull(resourceScalingStore, nameof(resourceScalingStore));
            ContinuationTaskActivator = Requires.NotNull(continuationTaskActivator, nameof(continuationTaskActivator));
            TaskHelper = Requires.NotNull(taskHelper, nameof(taskHelper));
            Mapper = Requires.NotNull(mapper, nameof(mapper));
        }

        private IResourcePoolManager ResourcePool { get; }

        private IResourcePoolDefinitionStore ResourceScalingStore { get; }

        private IContinuationTaskActivator ContinuationTaskActivator { get; }

        private ITaskHelper TaskHelper { get; }

        private IMapper Mapper { get; }

        /// <inheritdoc/>
        public Task<AllocateResult> AllocateAsync(AllocateInput input, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_allocate",
                async (childLogger) =>
                {
                    // Setting up logger
                    childLogger.FluentAddBaseValue("ResourceLocation", input.Location.ToString())
                        .FluentAddBaseValue("ResourceSystemSkuName", input.SkuName)
                        .FluentAddBaseValue("ResourceType", input.Type.ToString());

                    // Map logical sku to resource sku
                    var resourceSku = await MapLogicalSkuToResourceSku(input.SkuName, input.Type, input.Location);

                    childLogger.FluentAddBaseValue("ResourceResourceSkuName", resourceSku.Details.SkuName);

                    // Try and get item from the pool
                    var item = await ResourcePool.TryGetAsync(
                        resourceSku.Details.GetPoolDefinition(), logger.NewChildLogger());

                    childLogger.FluentAddBaseValue("ResourceResourceAllocateFound", item != null);

                    // Deal with case that it didn't exist
                    if (item == null)
                    {
                        throw new OutOfCapacityException(input.SkuName, input.Type, input.Location.ToString().ToLowerInvariant());
                    }

                    childLogger.FluentAddBaseValue("ResourceId", item.Id);

                    // Trigger auto pool create to replace assigned item
                    TaskHelper.RunBackground(
                        $"{LogBaseName}_run_create",
                        (taskLogger) => ContinuationTaskActivator.CreateResource(
                            Guid.NewGuid(), resourceSku.Type, resourceSku.Details, "ResourceAssignedReplace", taskLogger),
                        childLogger,
                        autoLogOperation: false);

                    return Mapper.Map<AllocateResult>(item);
                });
        }

        /// <inheritdoc/>
        public Task<DeallocateResult> DeallocateAsync(
            DeallocateInput input,
            IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_deallocate",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("ResourceId", input.ResourceId);

                    await ContinuationTaskActivator.DeleteResource(
                        input.ResourceId, input.Trigger, childLogger.NewChildLogger());

                    return new DeallocateResult { Successful = true };
                });
        }

        /// <inheritdoc/>
        public Task<EnvironmentStartResult> StartComputeAsync(
            EnvironmentStartInput input,
            IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_start_compute",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("ResourceId", input.ComputeResourceId)
                        .FluentAddBaseValue("StorageResourceId", input.StorageResourceId);

                    await ContinuationTaskActivator.StartEnvironment(
                        input.ComputeResourceId, input.StorageResourceId, input.EnvironmentVariables, input.Trigger, childLogger.NewChildLogger());

                    return new EnvironmentStartResult { Successful = true };
                });
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
