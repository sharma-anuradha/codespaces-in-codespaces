// <copyright file="AllocationOSDiskSnapshotStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Strategies
{
    /// <summary>
    /// Allocation strategy for OS disk snapshots.
    /// </summary>
    public class AllocationOSDiskSnapshotStrategy : IAllocationStrategy
    {
        private const string LogBaseName = ResourceLoggingConstants.ResourceBrokerAllocatorOSDiskSnapshot;

        /// <summary>
        /// Initializes a new instance of the <see cref="AllocationOSDiskSnapshotStrategy"/> class.
        /// </summary>
        /// <param name="resourceRepository">Resource repository.</param>
        /// <param name="clientFactory">Azure client factory.</param>
        /// <param name="taskHelper">Task helper.</param>
        /// <param name="mapper">Mapper.</param>
        public AllocationOSDiskSnapshotStrategy(
            IResourceRepository resourceRepository,
            IAzureClientFactory clientFactory,
            ITaskHelper taskHelper,
            IMapper mapper)
        {
            ResourceRepository = Requires.NotNull(resourceRepository, nameof(resourceRepository));
            ClientFactory = Requires.NotNull(clientFactory, nameof(clientFactory));
            TaskHelper = Requires.NotNull(taskHelper, nameof(taskHelper));
            Mapper = Requires.NotNull(mapper, nameof(mapper));
        }

        private IResourceRepository ResourceRepository { get; }

        private IAzureClientFactory ClientFactory { get; }

        private ITaskHelper TaskHelper { get; }

        private IMapper Mapper { get; }

        /// <inheritdoc/>
        public async Task<IEnumerable<AllocateResult>> AllocateAsync(
            Guid environmentId,
            IEnumerable<AllocateInput> inputs,
            string trigger,
            IDiagnosticsLogger logger,
            IDictionary<string, string> loggingProperties = null)
        {
            var result = await Task.WhenAll<AllocateResult>(inputs.Select(x => AllocateAsync(environmentId, x, trigger, logger)));
            return result.ToList();
        }

        /// <inheritdoc/>
        public Task<AllocateResult> AllocateAsync(Guid environmentId, AllocateInput input, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}-allocate",
                async (childLogger) =>
                {
                    if (input.Type == ResourceType.Snapshot)
                    {
                        return await AllocateSnapshotAsync(environmentId, input, trigger, logger);
                    }
                    else if (input.Type == ResourceType.OSDisk)
                    {
                        return await AllocateDiskAsync(environmentId, input, trigger, logger);
                    }

                    throw new ArgumentException($"Don't know how to allocate resource of {input.Type}, missing or wrong arguments");
                });
        }

        /// <inheritdoc/>
        public bool CanHandle(IEnumerable<AllocateInput> inputs)
        {
            return inputs.All(x =>
            {
                return (x.Type == ResourceType.Snapshot && !string.IsNullOrEmpty(x.ExtendedProperties.OSDiskResourceID))
                      || (x.Type == ResourceType.OSDisk && !string.IsNullOrEmpty(x.ExtendedProperties.OSDiskSnapshotResourceID));
            });
        }

        private Task<AllocateResult> AllocateSnapshotAsync(Guid environmentId, AllocateInput input, string trigger, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_allocate_snapshot",
                async (childLogger) =>
                {
                    // Get disk details
                    if (string.IsNullOrEmpty(input.ExtendedProperties.OSDiskResourceID))
                    {
                        throw new MissingFieldException("Missing OS disk ID in disk snapshot request");
                    }

                    // Get disk details and Azure client
                    var diskRecord = await ResourceRepository.GetAsync(input.ExtendedProperties.OSDiskResourceID, logger.NewChildLogger());
                    if (diskRecord == null)
                    {
                        throw new NotFoundException($"Disk with ID {input.ExtendedProperties.OSDiskResourceID} not found");
                    }

                    // Retrieve Azure client
                    var azure = await ClientFactory.GetAzureClientAsync(diskRecord.AzureResourceInfo.SubscriptionId);
                    if (azure == null)
                    {
                        throw new InvalidDataException($"Can't retrieve Azure client for {diskRecord.AzureResourceInfo.SubscriptionId} subscription");
                    }

                    // Create snapshot from disk
                    var disk = await azure.Disks.GetByResourceGroupAsync(diskRecord.AzureResourceInfo.ResourceGroup, diskRecord.AzureResourceInfo.Name);

                    childLogger.FluentAddValue("SnapshotDiskTargetSizeGb", disk.SizeInGB)
                        .FluentAddValue("SnapshotDiskOSType", disk.OSType)
                        .FluentAddValue("ResourceLocation", disk.RegionName)
                        .FluentAddValue("ResourceGroup", disk.ResourceGroupName)
                        .FluentAddValue("ResourceType", ResourceType.Snapshot.ToString());

                    var snapshot = await azure.Snapshots.Define($"{Guid.NewGuid()}-snapshot")
                        .WithRegion(disk.RegionName)
                        .WithExistingResourceGroup(disk.ResourceGroupName)
                        .WithDataFromDisk(disk.Id)
                        .CreateAsync();
                    if (snapshot == null)
                    {
                        throw new ProcessingFailedException($"Could not create snapshot from disk {input.ExtendedProperties.OSDiskResourceID}");
                    }

                    var snapshotRecord = await CreateOSDiskSnapshotRecord(Guid.Parse(snapshot.Key), snapshot.Name, input.Location, diskRecord, childLogger);

                    return Mapper.Map<AllocateResult>(snapshotRecord);
                });
        }

        private Task<AllocateResult> AllocateDiskAsync(Guid environmentId, AllocateInput input, string trigger, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_allocate_disk",
                async (childLogger) =>
                {
                    // Get snapshot details
                    if (string.IsNullOrEmpty(input.ExtendedProperties.OSDiskSnapshotResourceID))
                    {
                        throw new MissingFieldException("Missing OS disk snapshot ID in disk allocation request");
                    }

                    var snapshotRecord = await ResourceRepository.GetAsync(input.ExtendedProperties.OSDiskSnapshotResourceID, childLogger.NewChildLogger());
                    if (snapshotRecord == null)
                    {
                        throw new NotFoundException($"Snapshot with ID {input.ExtendedProperties.OSDiskSnapshotResourceID} not found");
                    }

                    // Retrieve Azure client
                    var azure = await ClientFactory.GetAzureClientAsync(snapshotRecord.AzureResourceInfo.SubscriptionId);
                    if (azure == null)
                    {
                        throw new InvalidDataException($"Can't retrieve Azure client for {snapshotRecord.AzureResourceInfo.SubscriptionId} subscription");
                    }

                    // Create snapshot from disk
                    var snapshot = await azure.Snapshots.GetByResourceGroupAsync(snapshotRecord.AzureResourceInfo.ResourceGroup, snapshotRecord.AzureResourceInfo.Name);

                    childLogger.FluentAddValue("ResourceLocation", snapshot.RegionName)
                        .FluentAddValue("ResourceGroup", snapshot.ResourceGroupName)
                        .FluentAddValue("ResourceType", ResourceType.OSDisk.ToString());

                    var disk = await azure.Disks.Define($"{Guid.NewGuid()}-disk")
                        .WithRegion(snapshot.RegionName)
                        .WithExistingResourceGroup(snapshot.ResourceGroupName)
                        .WithWindowsFromSnapshot(snapshot)
                        .CreateAsync();
                    if (disk == null)
                    {
                        throw new ProcessingFailedException($"Could not create disk from snapshot {input.ExtendedProperties.OSDiskSnapshotResourceID}");
                    }

                    var diskRecord = await CreateOSDiskRecord(Guid.Parse(disk.Key), disk.Name, input.Location, snapshotRecord, childLogger);

                    // Remove snapshot as a background task
                    TaskHelper.RunBackground(
                        $"{LogBaseName}_snapshot_delete",
                        async (IDiagnosticsLogger innerLogger) =>
                        {
                            await azure.Snapshots.DeleteByResourceGroupAsync(snapshotRecord.AzureResourceInfo.ResourceGroup, snapshotRecord.AzureResourceInfo.Name);
                            await ResourceRepository.DeleteAsync(input.ExtendedProperties.OSDiskSnapshotResourceID, childLogger.NewChildLogger());

                            diskRecord.IsReady = true;
                            diskRecord.ProvisioningStatus = diskRecord.StartingStatus = OperationState.Succeeded;
                            diskRecord.ProvisioningStatusChanged = diskRecord.StartingStatusChanged = diskRecord.Ready = DateTime.UtcNow;
                        });

                    return Mapper.Map<AllocateResult>(diskRecord);
                });
        }

        private Task<ResourceRecord> CreateOSDiskSnapshotRecord(Guid id, string snapshotName, AzureLocation location, ResourceRecord diskRecord, IDiagnosticsLogger logger)
        {
            return logger.RetryOperationScopeAsync(
                $"{LogBaseName}_snapshot_record_create",
                async (childLogger) =>
                {
                    var time = DateTime.UtcNow;
                    var type = ResourceType.Snapshot;
                    var skuName = nameof(ResourceType.Snapshot);

                    // Core record
                    var resource = ResourceRecord.Build(id, time, type, location, skuName);
                    resource.AzureResourceInfo = new AzureResourceInfo(diskRecord.AzureResourceInfo.SubscriptionId, diskRecord.AzureResourceInfo.ResourceGroup, snapshotName);
                    resource.IsAssigned = true;
                    resource.Assigned = time;
                    resource.IsReady = true;
                    resource.Ready = resource.ProvisioningStatusChanged = time;
                    resource.ProvisioningStatus = resource.StartingStatus = OperationState.Succeeded;
                    resource.ProvisioningStatusChanged = resource.StartingStatusChanged = time;

                    // Create the actual record
                    resource = await ResourceRepository.CreateAsync(resource, logger.NewChildLogger());
                    return resource;
                });
        }

        private Task<ResourceRecord> CreateOSDiskRecord(Guid id, string diskName, AzureLocation location, ResourceRecord snapshotRecord, IDiagnosticsLogger logger)
        {
            return logger.RetryOperationScopeAsync(
                $"{LogBaseName}_disk_record_create",
                async (childLogger) =>
                {
                    var time = DateTime.UtcNow;
                    var type = ResourceType.OSDisk;
                    var skuName = nameof(ResourceType.OSDisk);

                    // Core record
                    var resource = ResourceRecord.Build(id, time, type, location, skuName);
                    resource.AzureResourceInfo = new AzureResourceInfo(snapshotRecord.AzureResourceInfo.SubscriptionId, snapshotRecord.AzureResourceInfo.ResourceGroup, diskName);
                    resource.IsAssigned = true;
                    resource.Assigned = time;
                    resource.IsReady = false;
                    resource.ProvisioningStatus = OperationState.InProgress;
                    resource.ProvisioningStatusChanged = time;

                    // Create the actual record
                    resource = await ResourceRepository.CreateAsync(resource, logger.NewChildLogger());
                    return resource;
                });
        }
    }
}