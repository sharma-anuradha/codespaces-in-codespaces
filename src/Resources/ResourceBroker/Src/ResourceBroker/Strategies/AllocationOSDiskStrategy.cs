// <copyright file="AllocationOSDiskStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Management.Batch.Fluent.Models;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Abstractions;
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
        /// <param name="agentSettings">Agent settings.</param>
        /// <param name="computeProvider">Compute provider.</param>
        public AllocationOSDiskStrategy(
            IResourceRepository resourceRepository,
            IResourcePoolManager resourcePool,
            IResourcePoolDefinitionStore resourceScalingStore,
            IResourceContinuationOperations resourceContinuationOperations,
            ITaskHelper taskHelper,
            IMapper mapper,
            IDiskProvider diskProvider,
            AgentSettings agentSettings,
            IComputeProvider computeProvider)
        {
            ResourceRepository = Requires.NotNull(resourceRepository, nameof(resourceRepository));
            ResourcePool = Requires.NotNull(resourcePool, nameof(resourcePool));
            ResourceScalingStore = Requires.NotNull(resourceScalingStore, nameof(resourceScalingStore));
            ResourceContinuationOperations = Requires.NotNull(resourceContinuationOperations, nameof(resourceContinuationOperations));
            TaskHelper = Requires.NotNull(taskHelper, nameof(taskHelper));
            Mapper = Requires.NotNull(mapper, nameof(mapper));
            DiskProvider = Requires.NotNull(diskProvider, nameof(diskProvider));
            AgentSettings = Requires.NotNull(agentSettings, nameof(agentSettings));
            ComputeProvider = Requires.NotNull(computeProvider, nameof(computeProvider));
        }

        private IResourceRepository ResourceRepository { get; }

        private IResourcePoolManager ResourcePool { get; }

        private IResourcePoolDefinitionStore ResourceScalingStore { get; }

        private IResourceContinuationOperations ResourceContinuationOperations { get; }

        private ITaskHelper TaskHelper { get; }

        private IMapper Mapper { get; }

        private IDiskProvider DiskProvider { get; }

        private AgentSettings AgentSettings { get; }

        private IComputeProvider ComputeProvider { get; }

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
                                computeRequest.ExtendedProperties,
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
                            computeRequest.ExtendedProperties.OSDiskResourceID = osDiskRequest.ExtendedProperties.OSDiskResourceID;
                            allocationResults = await TryQueueComputeWithExistingOSDisk(
                                resourceSku,
                                trigger,
                                computeRequest.ExtendedProperties,
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
            var diskResourceResult = await DiskProvider.AcquireOSDiskAsync(
                new DiskProviderAcquireOSDiskInput()
                {
                    VirtualMachineResourceInfo = computeResource.AzureResourceInfo,
                    AzureVmLocation = computeResource.Location.ToEnum<AzureLocation>(),
                    OSDiskResourceTags = osDiskResource.GetResourceTags(reason),
                    AdditionalComputeResourceTags = computeResourceTags,
                },
                logger.NewChildLogger());

            await logger.RetryOperationScopeAsync(
                    $"{LogBaseName}_osdisk_record_update",
                    async (IDiagnosticsLogger innerLogger) =>
                    {
                        // Update disk azure resource info.
                        osDiskResource.AzureResourceInfo = diskResourceResult.AzureResourceInfo;

                        // Copy queue info.
                        osDiskResource = PreserveQueueAndCopyComponent(computeResource, osDiskResource);

                        // Copy provisioning and ready status.
                        osDiskResource.ProvisioningReason = computeResource.ProvisioningReason;
                        osDiskResource.ProvisioningStatus = computeResource.ProvisioningStatus;
                        osDiskResource.IsReady = computeResource.IsReady;
                        osDiskResource.Ready = computeResource.Ready;

                        osDiskResource = await ResourceRepository.UpdateAsync(osDiskResource, logger.NewChildLogger());
                    });

            // Update compute record with disk components
            await logger.RetryOperationScopeAsync(
                    $"{LogBaseName}_compute_record_update",
                    async (IDiagnosticsLogger innerLogger) =>
                    {
                        computeResource = await ResourceRepository.GetAsync(computeResource.Id, innerLogger.NewChildLogger());

                        computeResource.Components.Items[osDiskResource.Id] = new ResourceComponent(
                                                                                    ResourceType.OSDisk,
                                                                                    osDiskResource.AzureResourceInfo,
                                                                                    osDiskResource.Id,
                                                                                    preserve: true);

                        var queueComponent = computeResource.Components.Items.Single(x => x.Value.ComponentType == ResourceType.InputQueue);
                        queueComponent.Value.Preserve = true;

                        computeResource = await ResourceRepository.UpdateAsync(computeResource, innerLogger.NewChildLogger());
                    });

            // Updates ComputeVM tags to include OS Disk record id.
            await ComputeProvider.UpdateTagsAsync(
               new VirtualMachineProviderUpdateTagsInput()
               {
                   VirtualMachineResourceInfo = computeResource.AzureResourceInfo,
                   CustomComponents = computeResource.Components?.Items?.Values.ToList(),
                   AdditionalComputeResourceTags = computeResourceTags,
               },
               logger.NewChildLogger());

            return (computeResource, osDiskResource);
        }

        private async Task<(ResourceRecord computeRecord, ResourceRecord osDiskRecord)> TryQueueComputeWithExistingOSDisk(
            ResourcePool resourceSku,
            string reason,
            AllocateExtendedProperties extendedProperties,
            IDiagnosticsLogger logger)
        {
            var osDiskResource = await ResourceRepository.GetAsync(extendedProperties.OSDiskResourceID, logger.NewChildLogger());

            if (!await DiskProvider.IsDetachedAsync(osDiskResource.AzureResourceInfo, logger.NewChildLogger()))
            {
                throw new InvalidOperationException($"OS disk is attached to a VM.");
            }

            extendedProperties.UpdateAgent = ShouldUpdateAgent(extendedProperties, osDiskResource);

            var computeResource = await ResourceContinuationOperations.QueueCreateAsync(
                Guid.NewGuid(),
                ResourceType.ComputeVM,
                extendedProperties,
                resourceSku.Details,
                reason,
                logger.NewChildLogger());

            osDiskResource = await ResourceRepository.GetAsync(extendedProperties.OSDiskResourceID, logger.NewChildLogger());

            return (computeResource, osDiskResource);
        }

        private bool ShouldUpdateAgent(AllocateExtendedProperties extendedProperties, ResourceRecord osDiskResource)
        {
            if (extendedProperties.UpdateAgent)
            {
                return extendedProperties.UpdateAgent;
            }

            if (osDiskResource.HeartBeatSummary?.LatestRawHeartBeat?.AgentVersion == default)
            {
                // Older environment will automatically follow agent update path.
                return true;
            }

            var minimumAgentVersion = Version.Parse(AgentSettings.MinimumVersion);
            var currentAgentVersion = Version.Parse(osDiskResource.HeartBeatSummary.LatestRawHeartBeat.AgentVersion);
            if (minimumAgentVersion > currentAgentVersion)
            {
                return true;
            }

            return false;
        }

        private async Task<(ResourceRecord computeRecord, ResourceRecord osDiskRecord)> TryQueueComputeAndOSDisk(
            ResourcePool resourceSku,
            AllocateExtendedProperties computeExtendedProperties,
            string reason,
            IDiagnosticsLogger logger)
        {
            var osDiskResource = await CreateOSDiskRecord(resourceSku, logger.NewChildLogger());
            computeExtendedProperties.OSDiskResourceID = osDiskResource.Id;

            var computeResource = await ResourceContinuationOperations.QueueCreateAsync(
                Guid.NewGuid(),
                ResourceType.ComputeVM,
                computeExtendedProperties,
                resourceSku.Details,
                reason,
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

        private ResourceRecord PreserveQueueAndCopyComponent(ResourceRecord sourceRecord, ResourceRecord targetRecord)
        {
            var queueComponent = sourceRecord.Components?.Items.SingleOrDefault(x => x.Value.ComponentType == ResourceType.InputQueue);

            if (!queueComponent.Value.Value.Preserve)
            {
                queueComponent.Value.Value.Preserve = true;
            }

            if (targetRecord.Components == default)
            {
                targetRecord.Components = new ResourceComponentDetail();
            }

            if (targetRecord.Components.Items == default)
            {
                targetRecord.Components.Items = new Dictionary<string, ResourceComponent>();
            }

            if (queueComponent.HasValue)
            {
                targetRecord.Components.Items[queueComponent.Value.Key] = queueComponent.Value.Value;
            }

            return targetRecord;
        }
    }
}
