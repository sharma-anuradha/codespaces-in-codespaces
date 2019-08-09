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
        /// <param name="resourcePool"></param>
        /// <param name="mapper"></param>
        public ResourceBroker(IResourcePool resourcePool, IMapper mapper)
        {
            ResourcePool = Requires.NotNull(resourcePool, nameof(resourcePool));
            Mapper = Requires.NotNull(mapper, nameof(mapper));
        }

        private IResourcePool ResourcePool { get; }

        private IMapper Mapper { get; }

        /// <inheritdoc/>
        public async Task<AllocateResult> AllocateAsync(AllocateInput input, IDiagnosticsLogger logger)
        {
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
        public Task StartComputeAsync(string computeResourceIdToken, string storageResourceIdToken, Dictionary<string, string> environmentVariables, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }
    }
}
