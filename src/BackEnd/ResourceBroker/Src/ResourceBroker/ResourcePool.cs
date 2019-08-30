// <copyright file="ResourcePool.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
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
            IResourceRepository resourceRepository)
        {
            ResourceRepository = Requires.NotNull(resourceRepository, nameof(resourceRepository));
        }

        private IResourceRepository ResourceRepository { get; }

        /// <inheritdoc/>
        public async Task<ResourceRecord> TryGetAsync(string skuName, ResourceType type, string location, IDiagnosticsLogger logger)
        {
            // Setup logging
            var duration = logger.StartDuration();

            var trys = 0;
            var item = (ResourceRecord)null;
            while (item == null && trys < 3)
            {
                // Setup logging
                var instanceDuration = logger.StartDuration();
                var instanceLogger = logger.WithValue("PoolLookupTry", trys.ToString());

                // Get core resource record
                item = await ResourceRepository.GetUnassignedResourceAsync(skuName, type, location, logger);

                // Break out if nothing is found
                if (item == null)
                {
                    // Log lookup miss
                    instanceLogger
                        .FluentAddValue("PoolLookupTryDuration", instanceDuration.ToString())
                        .AddDuration(duration)
                        .LogInfo($"resource_pool_lookup_miss");

                    break;
                }

                try
                {
                    // Update core properties to indicate that its assigned
                    item.IsAssigned = true;
                    item.Assigned = DateTime.UtcNow;

                    // Update core resource record
                    await ResourceRepository.UpdateAsync(item, logger);

                    // Log lookup found
                    instanceLogger
                        .FluentAddValue("PoolLookupTryDuration", instanceDuration.ToString())
                        .AddDuration(duration)
                        .LogInfo($"resource_pool_lookup_found");

                    break;
                }
                catch (DocumentClientException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
                {
                    // Log lookup found
                    instanceLogger
                        .FluentAddValue("PoolLookupTryDuration", instanceDuration.ToString())
                        .AddDuration(duration)
                        .LogInfo($"resource_pool_lookup_conflict");

                    item = null;
                }

                trys++;
            }

            return item;
        }
    }
}
