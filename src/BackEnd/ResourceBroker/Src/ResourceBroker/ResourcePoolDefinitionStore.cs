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
    /// Store which provides access to the Resource Pool Deginitions.
    /// </summary>
    public class ResourcePoolDefinitionStore : IResourcePoolDefinitionStore, IResourceScalingHandler
    {
        private IEnumerable<ResourcePool> ResourceScaleLevels { get; set; }

        /// <inheritdoc/>
        public Task<ScalingResult> UpdateResourceScaleLevels(ScalingInput scalingInputs)
        {
            // Persist the result
            ResourceScaleLevels = scalingInputs.Pools;

            return Task.FromResult(new ScalingResult());
        }

        /// <inheritdoc/>
        public Task<IEnumerable<ResourcePool>> RetrieveDefinitions()
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
