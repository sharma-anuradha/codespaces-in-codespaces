// <copyright file="ResourcePool.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// 
    /// </summary>
    public class ResourcePool : IResourcePool
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePool"/> class.
        /// </summary>
        /// <param name="resourceRepository"></param>
        /// <param name="resourceScalingStore"></param>
        public ResourcePool(
            IResourceRepository resourceRepository,
            IResourceScalingStore resourceScalingStore)
        {
            ResourceRepository = Requires.NotNull(resourceRepository, nameof(resourceRepository));
            ResourceScalingStore = Requires.NotNull(resourceScalingStore, nameof(resourceScalingStore));
        }

        private IResourceRepository ResourceRepository { get; }

        private IResourceScalingStore ResourceScalingStore { get; }

        /// <inheritdoc/>
        public async Task<ResourceRecord> TryGetAsync(AllocateInput input, IDiagnosticsLogger logger)
        {
            // Map logical sku to resource sku
            var resourceSku = await MapLogicalSkuToResourceSku(input.SkuName, input.Type, input.Location);

            var trys = 0;
            var item = (ResourceRecord)null;
            while (item == null && trys < 3)
            {
                // Get core resource record
                item = await ResourceRepository.GetUnassignedResourceAsync(resourceSku, input.Type, input.Location, logger);

                // Break out if nothing is found
                if (item == null)
                {
                    break;
                }

                try
                {
                    // Update core properties to indicate that its assigned
                    item.IsAssigned = true;
                    item.Assigned = DateTime.UtcNow;

                    // Update core resource record
                    await ResourceRepository.UpdateAsync(item, logger);

                    break;
                }
                catch (DocumentClientException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
                {
                    // TODO: Log that this condition has been meet, and go again... need to track
                    //       how offten this happens so that we can see  if 3 retries is enough, etc.

                    item = null;
                }

                trys++;
            }

            return item;
        }

        private async Task<string> MapLogicalSkuToResourceSku(string skuName, ResourceType type, string location)
        {
            var resources = await ResourceScalingStore.RetrieveLatestScaleLevels();

            var resourceSku = resources
                .Where(x => x.Location == location
                    && x.Type == type
                    && x.EnvironmentSkus.Contains(skuName))
                .FirstOrDefault();

            if (resourceSku == null)
            {
                throw new Exception("Sku resource match was not found.");
            }

            return resourceSku.SkuName;
        }
    }
}
