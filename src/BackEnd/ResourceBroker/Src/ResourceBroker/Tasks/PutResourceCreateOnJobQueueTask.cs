// <copyright file="ResourceManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Manager that coordinates resource orchistration efforts.
    /// </summary>
    public class PutResourceCreateOnJobQueueTask : IPutResourceCreateOnJobQueueTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PutResourceCreateOnJobQueueTask"/> class.
        /// </summary>
        /// <param name="resourceRepository">Resource repository that should be used.</param>
        /// <param name="resourceJobQueueRepository">Resource Job Queue repository that should be used.</param>
        public PutResourceCreateOnJobQueueTask(
            IResourceRepository resourceRepository,
            IResourceJobQueueRepository resourceJobQueueRepository)
        {
            ResourceRepository = Requires.NotNull(resourceRepository, nameof(resourceRepository));
            ResourceJobQueueRepository = Requires.NotNull(resourceJobQueueRepository, nameof(resourceJobQueueRepository));
        }

        private IResourceRepository ResourceRepository { get; }

        private IResourceJobQueueRepository ResourceJobQueueRepository { get; }

        /// <inheritdoc/>
        public async Task RunAsync(
            string skuName,
            ResourceType type,
            string location,
            IDiagnosticsLogger logger)
        {
            // Build new resource record
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

            // Create the resource record
            await ResourceRepository.CreateAsync(record, logger);

            // Add record to queue so that it can be picked up and processed
            await ResourceJobQueueRepository.AddAsync(id, logger);
        }
    }
}
