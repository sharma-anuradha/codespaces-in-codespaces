// <copyright file="ResourceScalingStoreExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions
{
    /// <summary>
    /// Resource scaling store extensions.
    /// </summary>
    public static class ResourceScalingStoreExtensions
    {
        /// <summary>
        /// Gets resource pool matching the criteria.
        /// </summary>
        /// <param name="resourceScalingStore">Resource pool definition.</param>
        /// <param name="skuName">Sku name.</param>
        /// <param name="type">Resource type.</param>
        /// <param name="location">Azure location.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<ResourcePool> MapLogicalSkuToResourceSku(this IResourcePoolDefinitionStore resourceScalingStore, string skuName, ResourceType type, AzureLocation location)
        {
            var resources = await resourceScalingStore.RetrieveDefinitionsAsync();
            var resourceSku = resources
                .Where(x => x.Details.Location == location
                    && x.Type == type
                    && x.LogicalSkus.Contains(skuName));

            if (!resourceSku.Any())
            {
                throw new ArgumentException($"Sku resource match was not found. SkuName = '{skuName}', Type = '{type}', Location = '{location}'");
            }

            if (resourceSku.Count() > 1)
            {
                throw new ArgumentException($"More than one Sku resource match was found. SkuName = '{skuName}', Type = '{type}', Location = '{location}'");
            }

            return resourceSku.Single();
        }

        /// <summary>
        /// Gets resource pool matching the criteria.
        /// </summary>
        /// <param name="resourceScalingStore">Resource pool definition.</param>
        /// <param name="poolCode">Target pool code.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<ResourcePool> MapPoolCodeToResourceSku(this IResourcePoolDefinitionStore resourceScalingStore, string poolCode)
        {
            var resources = await resourceScalingStore.RetrieveDefinitionsAsync();
            var resourceSku = resources
                .Where(x => x.Id == poolCode);

            if (resourceSku.Count() > 1)
            {
                throw new ArgumentException($"More than one Sku resource match was found. poolCode = '{poolCode}'");
            }

            return resourceSku.SingleOrDefault();
        }
    }
}
