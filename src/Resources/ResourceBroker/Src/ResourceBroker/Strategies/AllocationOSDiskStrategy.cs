// <copyright file="AllocationOSDiskStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
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
        /// <param name="resourceScalingStore">Resource scaling store.</param>
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
            IDiagnosticsLogger logger,
            IDictionary<string, string> loggingProperties)
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
                                logger.NewChildLogger(),
                                loggingProperties);
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
                                logger.NewChildLogger(),
                                loggingProperties);
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
        public Task<AllocateResult> AllocateAsync(Guid environmentId, AllocateInput input, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties)
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

            var osDiskResource = await ResourceRepository.GetAsync(computeResource.GetComputeDetails().OSDiskRecordId, logger.NewChildLogger());
            await logger.RetryOperationScopeAsync(
                            $"{LogBaseName}_osdisk_get_update",
                            async (IDiagnosticsLogger innerLogger) =>
                            {
                                osDiskResource = await ResourceRepository.GetAsync(computeResource.GetComputeDetails().OSDiskRecordId, logger.NewChildLogger());

                                // Update core properties to indicate that its assigned
                                osDiskResource.IsAssigned = true;
                                osDiskResource.Assigned = DateTime.UtcNow;

                                osDiskResource = await ResourceRepository.UpdateAsync(osDiskResource, logger.NewChildLogger());
                            });

            return (computeResource, osDiskResource);
        }

        private async Task<(ResourceRecord computeRecord, ResourceRecord osDiskRecord)> TryQueueComputeWithExistingOSDisk(
            ResourcePool resourceSku,
            string reason,
            AllocateExtendedProperties extendedProperties,
            IDiagnosticsLogger logger,
            IDictionary<string, string> loggingProperties)
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
                logger.NewChildLogger(),
                loggingProperties);

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
            IDiagnosticsLogger logger,
            IDictionary<string, string> loggingProperties)
        {
            var osDiskResource = await CreateOSDiskRecord(resourceSku.Details as ResourcePoolComputeDetails, logger.NewChildLogger());
            computeExtendedProperties.OSDiskResourceID = osDiskResource.Id;

            var computeResource = await ResourceContinuationOperations.QueueCreateAsync(
                Guid.NewGuid(),
                ResourceType.ComputeVM,
                computeExtendedProperties,
                resourceSku.Details,
                reason,
                logger.NewChildLogger(),
                loggingProperties);

            return (computeResource, osDiskResource);
        }

        private async Task<ResourceRecord> CreateOSDiskRecord(ResourcePoolComputeDetails details, IDiagnosticsLogger logger)
        {
            var resource = details.CreateOSDiskRecord();
            resource.IsAssigned = true;
            resource.Assigned = DateTime.UtcNow;

            // Create the actual record
            resource = await ResourceRepository.CreateAsync(resource, logger.NewChildLogger());
            return resource;
        }
    }
}
