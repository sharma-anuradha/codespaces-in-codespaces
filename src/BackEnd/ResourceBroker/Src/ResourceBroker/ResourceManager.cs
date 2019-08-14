// <copyright file="ResourceManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Manager that coordinates resource orchistration efforts.
    /// </summary>
    public class ResourceManager : IResourceManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceManager"/> class.
        /// </summary>
        /// <param name="resourceRepository">Resource repository that should be used.</param>
        /// <param name="resourceJobQueueRepository">Resource Job Queue repository that should be used.</param>
        public ResourceManager(
            IResourceRepository resourceRepository,
            IResourceJobQueueRepository resourceJobQueueRepository)
        {
            ResourceRepository = Requires.NotNull(resourceRepository, nameof(resourceRepository));
            ResourceJobQueueRepository = Requires.NotNull(resourceJobQueueRepository, nameof(resourceJobQueueRepository));
        }

        private IResourceRepository ResourceRepository { get; }

        private IResourceJobQueueRepository ResourceJobQueueRepository { get; }

        /// <inheritdoc/>
        public async Task AddResourceCreationRequestToJobQueueAsync(
            string skuName,
            ResourceType type,
            string location,
            IDiagnosticsLogger logger)
        {
            // Add record to database so that it can be tracked
            var id = Guid.NewGuid().ToString();
            var time = DateTime.UtcNow;
            var record = new ResourceRecord
            {
                Id = id,
                SkuName = skuName,
                Type = type,
                Location = location.ToLowerInvariant(),
                IsReady = false,
                IsAssigned = false,
                Created = time,
            };
            record.UpdateProvisioningStatus(ResourceProvisioningStatus.Queued);

            await ResourceRepository.CreateAsync(record, logger);

            // Add record to queue so that it can be picked up and processed
            await ResourceJobQueueRepository.AddAsync(id, logger);
        }
    }
}
