// <copyright file="ResourceBroker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// 
    /// </summary>
    public class ResourceBroker : IResourceBroker
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceBroker"/> class.
        /// </summary>
        /// <param name="resourcePool">Resource pool that should be used.</param>
        /// <param name="startComputeTask">Start compute task that should be used.</param>
        /// <param name="mapper">Mapper that should be used.</param>
        public ResourceBroker(
            IResourcePool resourcePool,
            IResourceScalingStore resourceScalingStore,
            IStartComputeTask startComputeTask,
            IMapper mapper)
        {
            ResourcePool = Requires.NotNull(resourcePool, nameof(resourcePool));
            ResourceScalingStore = Requires.NotNull(resourceScalingStore, nameof(resourceScalingStore));
            StartComputeTask = Requires.NotNull(startComputeTask, nameof(startComputeTask));
            Mapper = Requires.NotNull(mapper, nameof(mapper));
        }

        private IResourcePool ResourcePool { get; }

        private IResourceScalingStore ResourceScalingStore { get; }

        private IStartComputeTask StartComputeTask { get; }

        private IMapper Mapper { get; }

        /// <inheritdoc/>
        public async Task<AllocateResult> AllocateAsync(AllocateInput input, IDiagnosticsLogger logger)
        {
            // Map logical sku to resource sku
            var resourceSku = await MapLogicalSkuToResourceSku(input.SkuName, input.Type, input.Location);

            // Try and get item from the pool
            var item = await ResourcePool.TryGetAsync(resourceSku, input.Type, input.Location, logger);
            if (item == null)
            {
                throw new OutOfCapacityException(input.SkuName, input.Type, input.Location);
            }

            return Mapper.Map<AllocateResult>(item);
        }

        /// <inheritdoc/>
        public Task<bool> DeallocateAsync(string resourceIdToken, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<EnvironmentStartResult> StartComputeAsync(
            EnvironmentStartInput input,
            IDiagnosticsLogger logger,
            string continuationToken = null)
        {
            // Start compute
            return await StartComputeTask.RunAsync(input, logger, continuationToken);
        }

        private async Task<string> MapLogicalSkuToResourceSku(string skuName, ResourceType type, string location)
        {
            var resources = await ResourceScalingStore.RetrieveLatestScaleLevels();

            var resourceSku = resources
                .Where(x => x.Location == location
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

            return resourceSku.Single().SkuName;
        }
    }
}
