﻿// <copyright file="AllocationOSDiskStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Strategies
{
    /// <summary>
    /// Allocation strategy for OS disks along with other resources.
    /// </summary>
    public class AllocationOSDiskStrategy : IAllocationStrategy
    {
        private const string LogBaseName = ResourceLoggingConstants.ResourceBrokerAllocatorOSDisk;

        /// <summary>
        /// Initializes a new instance of the <see cref="AllocationOSDiskStrategy"/> class.
        /// </summary>
        /// <param name="resourceRepository">Resource repository.</param>
        /// <param name="resourcePool">Resource pool.</param>
        /// <param name="resourceScalingStore">resource scaling store.</param>
        /// <param name="resourceContinuationOperations">Resource continuation operations.</param>
        /// <param name="taskHelper">Task helper.</param>
        /// <param name="mapper">Mapper.</param>
        /// <param name="diskProvider">Disk provider.</param>
        public AllocationOSDiskStrategy(
            IResourceRepository resourceRepository,
            IResourcePoolManager resourcePool,
            IResourcePoolDefinitionStore resourceScalingStore,
            IResourceContinuationOperations resourceContinuationOperations,
            ITaskHelper taskHelper,
            IMapper mapper,
            IDiskProvider diskProvider)
        {
            ResourceRepository = Requires.NotNull(resourceRepository, nameof(resourceRepository));
            ResourcePool = Requires.NotNull(resourcePool, nameof(resourcePool));
            ResourceScalingStore = Requires.NotNull(resourceScalingStore, nameof(resourceScalingStore));
            ResourceContinuationOperations = Requires.NotNull(resourceContinuationOperations, nameof(resourceContinuationOperations));
            TaskHelper = Requires.NotNull(taskHelper, nameof(taskHelper));
            Mapper = Requires.NotNull(mapper, nameof(mapper));
            DiskProvider = Requires.NotNull(diskProvider, nameof(diskProvider));
        }

        /// <summary>
        /// Gets resource repository.
        /// </summary>
        private IResourceRepository ResourceRepository { get; }

        /// <summary>
        /// Gets resource pool.
        /// </summary>
        private IResourcePoolManager ResourcePool { get; }

        /// <summary>
        /// Gets resource scaling store.
        /// </summary>
        private IResourcePoolDefinitionStore ResourceScalingStore { get; }

        /// <summary>
        /// Gets resource continuation operations.
        /// </summary>
        private IResourceContinuationOperations ResourceContinuationOperations { get; }

        /// <summary>
        /// Gets task helper.
        /// </summary>
        private ITaskHelper TaskHelper { get; }

        /// <summary>
        /// Gets mapper.
        /// </summary>
        private IMapper Mapper { get; }

        private IDiskProvider DiskProvider { get; }

        /// <inheritdoc/>
        public Task<IEnumerable<AllocateResult>> AllocateAsync(
            Guid environmentId,
            IEnumerable<AllocateInput> inputs,
            string trigger,
            IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_allocate_pair",
                async (childLogger) =>
                {
                    var osDiskRequest = inputs.Single(x => x.Type == ResourceType.OSDisk);
                    var computeRequest = inputs.Single(x => x.Type == ResourceType.ComputeVM);

                    var resourceSku = await ResourceScalingStore.MapLogicalSkuToResourceSku(computeRequest.SkuName, computeRequest.Type, computeRequest.Location);

                    var osDiskExists = !string.IsNullOrWhiteSpace(osDiskRequest.ExtendedProperties?.OSDiskResourceID);

                    var allocationResults = default((ResourceRecord computeRecord, ResourceRecord osDiskRecord));
                    if (!osDiskExists)
                    {
                        if (osDiskRequest.QueueCreateResource == false && computeRequest.QueueCreateResource == false)
                        {
                            allocationResults = await TryGetComputeAndOSDisk(resourceSku, trigger, logger.NewChildLogger());
                        }
                        else if (osDiskRequest.QueueCreateResource == true && computeRequest.QueueCreateResource == true)
                        {
                            allocationResults = await TryQueueComputeAndOSDisk(
                                resourceSku,
                                trigger,
                                logger.NewChildLogger());
                        }
                        else
                        {
                            throw new InvalidOperationException($"Not a supported allocation request.");
                        }
                    }
                    else
                    {
                        if (computeRequest.QueueCreateResource == true)
                        {
                            allocationResults = await TryQueueComputeWithExistingOSDisk(
                                resourceSku,
                                trigger,
                                osDiskRequest.ExtendedProperties.OSDiskResourceID,
                                logger.NewChildLogger());
                        }
                        else
                        {
                            throw new InvalidOperationException($"Not a supported allocation request. Compute request should be queued when OS disk exists.");
                        }
                    }

                    var results = new List<AllocateResult>
                    {
                        Mapper.Map<AllocateResult>(allocationResults.computeRecord),
                        Mapper.Map<AllocateResult>(allocationResults.osDiskRecord),
                    };

                    return (IEnumerable<AllocateResult>)results;
                });
        }

        /// <inheritdoc/>
        public Task<AllocateResult> AllocateAsync(Guid environmentId, AllocateInput input, string trigger, IDiagnosticsLogger logger)
        {
            throw new NotSupportedException($"Allocation of a single resource type '{input.Type}' not supported.");
        }

        /// <inheritdoc/>
        public bool CanHandle(IEnumerable<AllocateInput> inputs)
        {
            return
                inputs.Count(x => x.Type == ResourceType.ComputeVM) == 1 &&
                inputs.Count(x => x.Type == ResourceType.OSDisk) == 1;
        }

        private async Task<(ResourceRecord computeRecord, ResourceRecord osDiskRecord)> TryGetComputeAndOSDisk(ResourcePool resourceSku, string reason, IDiagnosticsLogger logger)
        {
            // Try and get item from the pool
            var computeResource = await ResourcePool.TryGetAsync(
                resourceSku.Details.GetPoolDefinition(), logger.NewChildLogger());

            if (computeResource == default)
            {
                throw new OutOfCapacityException(resourceSku.Details.SkuName, ResourceType.ComputeVM, resourceSku.Details.Location.ToString().ToLowerInvariant());
            }

            // Trigger auto pool create to replace assigned item
            TaskHelper.RunBackground(
                $"{LogBaseName}_run_create",
                (taskLogger) => ResourceContinuationOperations.CreateAsync(
                    Guid.NewGuid(), resourceSku.Type, resourceSku.Details, "ResourceAssignedReplace", taskLogger),
                logger);

            var osDiskResource = await CreateOSDiskRecord(resourceSku, logger.NewChildLogger());

            var computeResourceTags = new Dictionary<string, string>
            {
                [ResourceTagName.ResourceComponentRecordIds] = osDiskResource.Id,
            };

            // Acquire OS disk information.
            // Updates ComputeVM tags as well.
            var diskResourceResult = await DiskProvider.AcquireOSDiskAsync(
                new DiskProviderAcquireOSDiskInput()
                {
                    VirtualMachineResourceInfo = computeResource.AzureResourceInfo,
                    AzureVmLocation = computeResource.Location.ToEnum<AzureLocation>(),
                    OSDiskResourceTags = osDiskResource.GetResourceTags(reason),
                    AdditionalComputeResourceTags = computeResourceTags,
                },
                logger.NewChildLogger());

            // Update disk azure resource info.
            osDiskResource.AzureResourceInfo = diskResourceResult.AzureResourceInfo;

            // Copy provisioning and ready status.
            osDiskResource.ProvisioningReason = computeResource.ProvisioningReason;
            osDiskResource.ProvisioningStatus = computeResource.ProvisioningStatus;
            osDiskResource.IsReady = computeResource.IsReady;
            osDiskResource.Ready = computeResource.Ready;

            osDiskResource = await ResourceRepository.UpdateAsync(osDiskResource, logger.NewChildLogger());

            // Update compute record with disk components
            await logger.RetryOperationScopeAsync(
                    $"{LogBaseName}_compute_record_update",
                    async (IDiagnosticsLogger innerLogger) =>
                    {
                        computeResource = await ResourceRepository.GetAsync(computeResource.Id, innerLogger.NewChildLogger());

                        computeResource.AzureResourceInfo.Components = new List<ResourceComponent>()
                        {
                            new ResourceComponent(
                                ComponentType.OSDisk,
                                osDiskResource.AzureResourceInfo,
                                osDiskResource.Id),
                        };

                        computeResource = await ResourceRepository.UpdateAsync(computeResource, innerLogger.NewChildLogger());
                    });

            return (computeResource, osDiskResource);
        }

        private async Task<(ResourceRecord computeRecord, ResourceRecord osDiskRecord)> TryQueueComputeWithExistingOSDisk(
            ResourcePool resourceSku,
            string reason,
            string osDiskResourceId,
            IDiagnosticsLogger logger)
        {
            var osDiskResource = await ResourceRepository.GetAsync(osDiskResourceId, logger.NewChildLogger());

            if (!await DiskProvider.IsDetachedAsync(osDiskResource.AzureResourceInfo, logger.NewChildLogger()))
            {
                throw new InvalidOperationException($"OS disk is attached to a VM.");
            }

            var computeResource = await ResourceContinuationOperations.QueueCreateComputeAsync(
                Guid.NewGuid(),
                ResourceType.ComputeVM,
                resourceSku.Details,
                reason,
                osDiskResourceId,
                logger.NewChildLogger());

            osDiskResource = await ResourceRepository.GetAsync(osDiskResourceId, logger.NewChildLogger());

            return (computeResource, osDiskResource);
        }

        private async Task<(ResourceRecord computeRecord, ResourceRecord osDiskRecord)> TryQueueComputeAndOSDisk(
            ResourcePool resourceSku,
            string reason,
            IDiagnosticsLogger logger)
        {
            var osDiskResource = await CreateOSDiskRecord(resourceSku, logger.NewChildLogger());

            var computeResource = await ResourceContinuationOperations.QueueCreateComputeAsync(
                Guid.NewGuid(),
                ResourceType.ComputeVM,
                resourceSku.Details,
                reason,
                osDiskResource.Id,
                logger.NewChildLogger());

            return (computeResource, osDiskResource);
        }

        private async Task<ResourceRecord> CreateOSDiskRecord(ResourcePool resourceSku, IDiagnosticsLogger logger)
        {
            var id = Guid.NewGuid();
            var time = DateTime.UtcNow;
            var type = ResourceType.OSDisk;
            var location = resourceSku.Details.Location;
            var skuName = "OSDisk";

            // Core record
            var resource = ResourceRecord.Build(id, time, type, location, skuName);
            resource.IsAssigned = true;
            resource.Assigned = time;
            resource.IsReady = false;
            resource.ProvisioningStatus = OperationState.InProgress;

            // Copy over pool reference detail.
            resource.PoolReference = new ResourcePoolDefinitionRecord
            {
                Code = resourceSku.Details.GetPoolDefinition(),
                VersionCode = resourceSku.Details.GetPoolVersionDefinition(),
                Dimensions = resourceSku.Details.GetPoolDimensions(),
            };

            // Create the actual record
            resource = await ResourceRepository.CreateAsync(resource, logger.NewChildLogger());
            return resource;
        }
    }
}
