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
        /// <param name="startComputeTask">Start compute task that should be used.</param>
        /// <param name="mapper">Mapper that should be used.</param>
        public ResourceBroker(
            IResourcePoolManager resourcePool,
            IResourcePoolDefinitionStore resourceScalingStore,
            IContinuationTaskActivator continuationTaskActivator,
            IMapper mapper)
        {
            ResourcePool = Requires.NotNull(resourcePool, nameof(resourcePool));
            ResourceScalingStore = Requires.NotNull(resourceScalingStore, nameof(resourceScalingStore));
            ContinuationTaskActivator = Requires.NotNull(continuationTaskActivator, nameof(continuationTaskActivator));
            Mapper = Requires.NotNull(mapper, nameof(mapper));
        }

        private IResourcePoolManager ResourcePool { get; }

        private IResourcePoolDefinitionStore ResourceScalingStore { get; }

        private IContinuationTaskActivator ContinuationTaskActivator { get; }

        private IMapper Mapper { get; }

        /// <inheritdoc/>
        public Task<AllocateResult> AllocateAsync(AllocateInput input, IDiagnosticsLogger logger)
        {
            // Setting up logger
            logger.FluentAddBaseValue("ResourceLocation", input.Location.ToString())
                .FluentAddBaseValue("ResourceSystemSkuName", input.SkuName)
                .FluentAddBaseValue("ResourceType", input.Type.ToString());

            return logger.OperationScopeAsync(
                $"{LogBaseName}_allocate",
                async () =>
                {
                    // Map logical sku to resource sku
                    var resourceSku = await MapLogicalSkuToResourceSku(input.SkuName, input.Type, input.Location);

                    logger.FluentAddBaseValue("ResourceResourceSkuName", resourceSku.Details.SkuName);

                    // Try and get item from the pool
                    var item = await ResourcePool.TryGetAsync(resourceSku.Details.GetPoolDefinition(), logger.WithValues(new LogValueSet()));

                    logger.FluentAddBaseValue("ResourceResourceAllocateFound", item != null);

                    // Deal with case that it didn't exist
                    if (item == null)
                    {
                        throw new OutOfCapacityException(input.SkuName, input.Type, input.Location.ToString().ToLowerInvariant());
                    }

                    logger.FluentAddBaseValue("ResourceId", item.Id);

                    return Mapper.Map<AllocateResult>(item);
                });
        }

        /// <inheritdoc/>
        public async Task<DeallocateResult> DeallocateAsync(
            DeallocateInput input,
            IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("ResourceId", input.ResourceId);

            await ContinuationTaskActivator.DeleteResource(input.ResourceId, input.Trigger, logger);

            return new DeallocateResult { Successful = true };
        }

        /// <inheritdoc/>
        public async Task<EnvironmentStartResult> StartComputeAsync(
            EnvironmentStartInput input,
            IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("ResourceId", input.ComputeResourceId)
                .FluentAddBaseValue("StorageResourceId", input.StorageResourceId);

            await ContinuationTaskActivator.StartEnvironment(
                input.ComputeResourceId, input.StorageResourceId, input.EnvironmentVariables, input.Trigger, logger);

            return new EnvironmentStartResult { Successful = true };
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
