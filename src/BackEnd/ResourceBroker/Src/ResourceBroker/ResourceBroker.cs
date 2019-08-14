// <copyright file="ResourceBroker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.VsSaaS.Diagnostics;
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
            IStartComputeTask startComputeTask,
            IMapper mapper)
        {
            ResourcePool = Requires.NotNull(resourcePool, nameof(resourcePool));
            StartComputeTask = Requires.NotNull(startComputeTask, nameof(startComputeTask));
            Mapper = Requires.NotNull(mapper, nameof(mapper));
        }

        private IResourcePool ResourcePool { get; }

        private IStartComputeTask StartComputeTask { get; }

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
        public async Task<EnvironmentStartResult> StartComputeAsync(
            EnvironmentStartInput input,
            IDiagnosticsLogger logger,
            string continuationToken = null)
        {
            // Start compute
            return await StartComputeTask.RunAsync(input, logger, continuationToken);
        }
    }
}
