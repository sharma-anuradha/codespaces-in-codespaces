// <copyright file="ResourceBroker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;

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
        /// <param name="resourceManager">Resource that should be used</param>
        /// <param name="mapper">Mapper that should be used</param>
        public ResourceBroker(
            IResourcePool resourcePool,
            IResourceManager resourceManager,
            IMapper mapper)
        {
            ResourcePool = Requires.NotNull(resourcePool, nameof(resourcePool));
            ResourceManager = Requires.NotNull(resourceManager, nameof(resourceManager));
            Mapper = Requires.NotNull(mapper, nameof(mapper));
        }

        private IResourcePool ResourcePool { get; }

        private IResourceManager ResourceManager { get; }

        private IMapper Mapper { get; }

        /// <inheritdoc/>
        public async Task<AllocateResult> AllocateAsync(AllocateInput input, IDiagnosticsLogger logger)
        {
            // Try and get item from the pool
            var item = await ResourcePool.TryGetAsync(input, logger);
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
        public async Task StartComputeAsync(
            string computeResourceIdToken,
            string storageResourceIdToken,
            IDictionary<string, string> environmentVariables,
            IDiagnosticsLogger logger)
        {
            // Start compute
            await ResourceManager.StartComputeAsync(
                computeResourceIdToken,
                storageResourceIdToken,
                environmentVariables,
                logger);
        }
    }
}
