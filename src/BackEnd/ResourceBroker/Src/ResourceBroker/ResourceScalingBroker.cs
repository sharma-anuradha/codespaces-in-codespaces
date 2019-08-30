// <copyright file="ResourceScalingBroker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    ///
    /// </summary>
    public class ResourceScalingBroker : IResourceScalingBroker, IResourceScalingStore
    {
        public ResourceScalingBroker(IMapper mapper)
        {
            Mapper = mapper;
        }

        private IMapper Mapper { get; }

        private IEnumerable<ResourcePoolDefinition> ResourceScaleLevels { get; set; }

        /// <inheritdoc/>
        public Task<ScalingResult> UpdateResourceScaleLevels(IEnumerable<ScalingInput> scalingInputs)
        {
            // Persist the result
            ResourceScaleLevels = Mapper.Map<IEnumerable<ResourcePoolDefinition>>(scalingInputs);

            return Task.FromResult(new ScalingResult());
        }

        /// <inheritdoc/>
        public Task<IEnumerable<ResourcePoolDefinition>> RetrieveLatestScaleLevels()
        {
            // If we don't have a result, wait a short time to see if it comes
            if (ResourceScaleLevels == null)
            {
                throw new InvalidOperationException("Resource Scaling Levels have not been initialized during startup.");
            }

            return Task.FromResult(ResourceScaleLevels);
        }
    }
}
