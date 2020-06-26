// <copyright file="ResourcePoolDefinitionStore.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Store which provides access to the Resource Pool Definitions.
    /// </summary>
    public class ResourcePoolDefinitionStore : IResourcePoolDefinitionStore, IResourceScalingHandler
    {
        private IList<ResourcePool> ResourceScaleLevels { get; set; }

        /// <inheritdoc/>
        public Task UpdateResourceScaleLevels(ScalingInput scalingInputs)
        {
            // Persist the result
            ResourceScaleLevels = scalingInputs.Pools;

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IList<ResourcePool>> RetrieveDefinitionsAsync()
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
