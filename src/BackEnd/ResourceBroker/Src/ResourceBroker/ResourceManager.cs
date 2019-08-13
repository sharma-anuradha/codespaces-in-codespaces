// <copyright file="ResourceManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

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
        /// <param name="computeProvider">Compute provider that should be used.</param>
        /// <param name="mapper">Mapper to use when converting users.</param>
        public ResourceManager(
            IResourceRepository resourceRepository,
            IResourceJobQueueRepository resourceJobQueueRepository,
            IComputeProvider computeProvider,
            IMapper mapper)
        {
            ResourceRepository = Requires.NotNull(resourceRepository, nameof(resourceRepository));
            ResourceJobQueueRepository = Requires.NotNull(resourceJobQueueRepository, nameof(resourceJobQueueRepository));
            ComputeProvider = Requires.NotNull(computeProvider, nameof(computeProvider));
            Mapper = Requires.NotNull(mapper, nameof(mapper));
        }

        private IResourceRepository ResourceRepository { get; }

        private IResourceJobQueueRepository ResourceJobQueueRepository { get; }

        private IComputeProvider ComputeProvider { get; }

        private IMapper Mapper { get; }

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
                ProvisioningStatus = ResourceProvisioningStatus.Queued,
                ProvisioningStatusChanged = time,
                ProvisioningStatusChanges = new List<ResourceProvisioningStatusChanges>
                {
                    new ResourceProvisioningStatusChanges
                    {
                        Status = ResourceProvisioningStatus.Queued,
                        Time = time,
                    },
                },
                Created = time,
            };

            await ResourceRepository.CreateAsync(record, logger);

            // Add record to queue so that it can be picked up and processed
            await ResourceJobQueueRepository.AddAsync(id, logger);
        }

        /// <inheritdoc/>
        public async Task StartComputeAsync(
            string computeResourceIdToken,
            string storageResourceIdToken,
            IDictionary<string, string> environmentVariables,
            IDiagnosticsLogger logger)
        {
            var computeResourceId = ResourceId.Parse(computeResourceIdToken);
            var storageResourceId = ResourceId.Parse(storageResourceIdToken);

            var storageRecord = await ResourceRepository.GetAsync(storageResourceId.InstanceId.ToString(), logger);
            var storageFileShareInfo = Mapper.Map<ShareConnectionInfo>(storageRecord.Properties);

            var input = new VirtualMachineProviderStartComputeInput(
                computeResourceId,
                storageFileShareInfo,
                environmentVariables);

            await ComputeProvider.StartComputeAsync(input, null);
        }
    }
}
