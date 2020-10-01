// <copyright file="ResourceBroker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Resource broker.
    /// </summary>
    public class ResourceBroker : IResourceBroker
    {
        private const string LogBaseName = ResourceLoggingConstants.ResourceBroker;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceBroker"/> class.
        /// </summary>
        /// <param name="resourceRepository">Resource repository that should be used.</param>
        /// <param name="resourceContinuationOperations">Target continuation task sctivator.</param>
        /// <param name="allocationStrategies">Allocation strategies.</param>
        /// <param name="secretManager">Secret manager.</param>
        public ResourceBroker(
            IResourceRepository resourceRepository,
            IResourceContinuationOperations resourceContinuationOperations,
            IEnumerable<IAllocationStrategy> allocationStrategies,
            ISecretManager secretManager,
            IQueueProvider queueProvider)
        {
            ResourceRepository = Requires.NotNull(resourceRepository, nameof(resourceRepository));
            ResourceContinuationOperations = Requires.NotNull(resourceContinuationOperations, nameof(resourceContinuationOperations));
            AllocationStrategies = Requires.NotNull(allocationStrategies, nameof(allocationStrategies));
            SecretManager = Requires.NotNull(secretManager, nameof(secretManager));
            QueueProvider = Requires.NotNull(queueProvider, nameof(queueProvider));
        }

        private IResourceRepository ResourceRepository { get; }

        private IResourceContinuationOperations ResourceContinuationOperations { get; }

        /// <summary>
        /// Gets the allocation strategies.
        /// Note: Currently allocation strategies handle exclusive inputs.
        /// In future, when multiple allocators can handle the given set, make sure to pick the allocator predictably.
        /// </summary>
        private IEnumerable<IAllocationStrategy> AllocationStrategies { get; }

        private ISecretManager SecretManager { get; }

        private IQueueProvider QueueProvider { get; }

        /// <inheritdoc/>
        public Task<IEnumerable<AllocateResult>> AllocateAsync(
            Guid environmentId, IEnumerable<AllocateInput> inputs, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_allocate_set",
                async (childLogger) =>
                {
                    var allocationHandler = AllocationStrategies.FirstOrDefault(x => x.CanHandle(inputs));
                    if (allocationHandler == default)
                    {
                        throw new NotSupportedException("Inputs not supported.");
                    }

                    return await allocationHandler.AllocateAsync(environmentId, inputs, trigger, childLogger.NewChildLogger(), loggingProperties);
                });
        }

        /// <inheritdoc/>
        public Task<AllocateResult> AllocateAsync(
            Guid environmentId, AllocateInput input, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_allocate",
                async (childLogger) =>
                {
                    var allocationHandler = AllocationStrategies.FirstOrDefault(x => x.CanHandle(new[] { input }));
                    if (allocationHandler == default)
                    {
                        throw new NotSupportedException("Inputs not supported.");
                    }

                    return await allocationHandler.AllocateAsync(environmentId, input, trigger, logger, loggingProperties);
                });
        }

        /// <inheritdoc/>
        public Task<bool> StartAsync(
            Guid environmentId, StartAction action, IEnumerable<StartInput> resources, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_start",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("StartAction", action.ToString());

                    // Match resources to records
                    var backingResources = new List<(StartInput Resource, ResourceRecord Record)>();
                    foreach (var resource in resources)
                    {
                        var record = await ResourceRepository.GetAsync(resource.ResourceId.ToString(), logger.NewChildLogger());
                        if (record == default)
                        {
                            throw new ResourceNotFoundException(resource.ResourceId);
                        }

                        backingResources.Add((Resource: resource, Record: record));
                    }

                    // Switch between different actions
                    switch (action)
                    {
                        case StartAction.StartExport:
                        case StartAction.StartUpdate:

                            if (backingResources.Count == 2 || backingResources.Count == 3)
                            {
                                // Select target resorces
                                var computeResource = backingResources.Single(x => x.Record.Type == ResourceType.ComputeVM);
                                var osDiskResource = backingResources.SingleOrDefault(x => x.Record.Type == ResourceType.OSDisk);
                                var storageResource = backingResources.SingleOrDefault(x => x.Record.Type == ResourceType.StorageFileShare);
                                var archiveStorageResource = backingResources.SingleOrDefault(x => x.Record.Type == ResourceType.StorageArchive);

                                childLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, computeResource.Resource.ResourceId)
                                    .FluentAddBaseValue("StorageResourceId", storageResource.Resource?.ResourceId)
                                    .FluentAddBaseValue("OSDiskResourceId", osDiskResource.Resource?.ResourceId)
                                    .FluentAddBaseValue("ArchiveStorageResourceId", archiveStorageResource.Resource?.ResourceId);

                                var userSecrets = default(IEnumerable<UserSecretData>);

                                // If secrets are sent from Create/Resume request payload
                                if (computeResource.Resource.Secrets?.Any() == true)
                                {
                                    userSecrets = computeResource.Resource.Secrets;
                                }

                                if (action == StartAction.StartExport)
                                {
                                    // Trigger environment export
                                    await ResourceContinuationOperations.StartExportAsync(
                                        environmentId,
                                        computeResource.Resource.ResourceId,
                                        osDiskResource.Resource?.ResourceId,
                                        storageResource.Resource?.ResourceId,
                                        archiveStorageResource.Resource?.ResourceId,
                                        computeResource.Resource.Variables,
                                        userSecrets,
                                        trigger,
                                        childLogger.NewChildLogger(),
                                        loggingProperties);
                                }
                                else if (action == StartAction.StartUpdate)
                                {
                                    // Trigger system update
                                    await UpdateSystemAsync(computeResource.Resource.ResourceId, trigger, childLogger.NewChildLogger(), loggingProperties);
                                }
                            }
                            else
                            {
                                throw new NotSupportedException($"Start {action.ToString().ToLower()} action expects 2 resource and {resources.Count()} was supplied.");
                            }

                            break;

                        case StartAction.StartCompute:

                            if (backingResources.Count == 2 || backingResources.Count == 3)
                            {
                                // Select target resources
                                var computeResource = backingResources.Single(x => x.Record.Type == ResourceType.ComputeVM);
                                var osDiskResource = backingResources.SingleOrDefault(x => x.Record.Type == ResourceType.OSDisk);
                                var storageResource = backingResources.SingleOrDefault(x => x.Record.Type == ResourceType.StorageFileShare);
                                var archiveStorageResource = backingResources.SingleOrDefault(x => x.Record.Type == ResourceType.StorageArchive);

                                childLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, computeResource.Resource.ResourceId)
                                    .FluentAddBaseValue("StorageResourceId", storageResource.Resource?.ResourceId)
                                    .FluentAddBaseValue("OSDiskResourceId", osDiskResource.Resource?.ResourceId)
                                    .FluentAddBaseValue("ArchiveStorageResourceId", archiveStorageResource.Resource?.ResourceId);

                                var userSecrets = default(IEnumerable<UserSecretData>);

                                var devcontainerJson = computeResource.Resource.DevContainer;

                                // If secrets are sent from Create/Resume request payload
                                if (computeResource.Resource.Secrets?.Any() == true)
                                {
                                    userSecrets = computeResource.Resource.Secrets;
                                }
                                else if (computeResource.Resource.FilterSecrets?.PrioritizedSecretStoreResources != null &&
                                    computeResource.Resource.FilterSecrets.PrioritizedSecretStoreResources.Any())
                                {
                                    userSecrets = await SecretManager.GetApplicableSecretsAndValuesAsync(computeResource.Resource.FilterSecrets, childLogger);
                                }

                                // Trigger environment start
                                await ResourceContinuationOperations.StartEnvironmentAsync(
                                    environmentId,
                                    computeResource.Resource.ResourceId,
                                    osDiskResource.Resource?.ResourceId,
                                    storageResource.Resource?.ResourceId,
                                    archiveStorageResource.Resource?.ResourceId,
                                    computeResource.Resource.Variables,
                                    userSecrets,
                                    devcontainerJson,
                                    trigger,
                                    childLogger.NewChildLogger(),
                                    loggingProperties);
                            }
                            else
                            {
                                throw new NotSupportedException($"Start compute action expects 2 or 3 resources and {backingResources.Count} were supplied.");
                            }

                            break;
                        case StartAction.StartArchive:
                            if (backingResources.Count == 2)
                            {
                                // Select target resources
                                var blobResource = backingResources.Single(x => x.Record.Type == ResourceType.StorageArchive);
                                var storageResource = backingResources.Single(x => x.Record.Type == ResourceType.StorageFileShare);

                                // Perform archiving in blob storage
                                childLogger.FluentAddBaseValue("StorageResourceId", storageResource.Resource.ResourceId)
                                    .FluentAddBaseValue("ArchiveStorageResourceId", blobResource.Resource.ResourceId);

                                await ResourceContinuationOperations.StartArchiveAsync(
                                    environmentId,
                                    blobResource.Resource.ResourceId,
                                    storageResource.Resource.ResourceId,
                                    trigger,
                                    childLogger.NewChildLogger(),
                                    loggingProperties);
                            }
                            else
                            {
                                throw new NotSupportedException($"Archive storage action expects 2 resources but {backingResources.Count} were supplied.");
                            }

                            break;
                    }

                    return true;
                });
        }

        /// <inheritdoc/>
        public Task<bool> StartAsync(
            Guid environmentId, StartAction action, StartInput input, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            throw new NotSupportedException("No action type supports the starting of a single resource.");
        }

        /// <inheritdoc/>
        public Task<bool> SuspendAsync(
            Guid environmentId, IEnumerable<SuspendInput> inputs, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_suspend_set",
                async (childLogger) =>
                {
                    var results = await Task.WhenAll(
                        inputs.Select(input => SuspendAsync(environmentId, input, trigger, childLogger.NewChildLogger(), loggingProperties)));

                    return results.All(x => x);
                });
        }

        /// <inheritdoc/>
        public Task<bool> SuspendAsync(
            Guid environmentId, SuspendInput input, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_suspend",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, input.ResourceId);

                    await ResourceContinuationOperations.SuspendAsync(
                        environmentId, input.ResourceId, trigger, logger, loggingProperties);

                    return true;
                });
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(
            Guid environmentId, IEnumerable<DeleteInput> inputs, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_delete_set",
                async (childLogger) =>
                {
                    var results = await Task.WhenAll(
                        inputs.Select(input => DeleteAsync(environmentId, input, trigger, childLogger.NewChildLogger(), loggingProperties)));

                    return results.All(x => x);
                });
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(
            Guid environmentId, DeleteInput input, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_delete",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, input.ResourceId);

                    await ResourceContinuationOperations.DeleteAsync(
                        environmentId, input.ResourceId, trigger, childLogger.NewChildLogger(), loggingProperties);

                    return true;
                });
        }

        /// <inheritdoc/>
        public Task<IEnumerable<StatusResult>> StatusAsync(
            Guid environmentId, IEnumerable<StatusInput> inputs, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_status_set",
                async (childLogger) =>
                {
                    var results = await Task.WhenAll(
                        inputs.Select(input => StatusAsync(environmentId, input, trigger, childLogger.NewChildLogger(), loggingProperties)));

                    return results.AsEnumerable();
                });
        }

        /// <inheritdoc/>
        public Task<StatusResult> StatusAsync(
            Guid environmentId, StatusInput input, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_status",
                async (childLogger) =>
                {
                    // Get record from db
                    var record = await ResourceRepository.GetAsync(input.ResourceId.ToString(), logger.NewChildLogger());
                    var assignedRecord = record;
                    if (record?.AssignedResourceId != default)
                    {
                        assignedRecord = await ResourceRepository.GetAsync(record.AssignedResourceId, logger.NewChildLogger());
                    }

                    if (assignedRecord == default)
                    {
                        return default;
                    }

                    var recordDetails = assignedRecord.GetDetails();

                    // Build result
                    var result = new StatusResult()
                    {
                        ResourceId = Guid.Parse(assignedRecord.Id),
                        SkuName = recordDetails.SkuName,
                        Location = recordDetails.Location,
                        Type = assignedRecord.Type,
                        IsReady = assignedRecord.IsReady,
                        Created = assignedRecord.Created,
                        ProvisioningStatus = assignedRecord.ProvisioningStatus,
                        ProvisioningStatusChanged = assignedRecord.ProvisioningStatusChanged,
                        StartingStatus = assignedRecord.StartingStatus,
                        StartingStatusChanged = assignedRecord.StartingStatusChanged,
                        DeletingStatus = assignedRecord.DeletingStatus,
                        DeletingStatusChanged = assignedRecord.DeletingStatusChanged,
                        CleanupStatus = assignedRecord.CleanupStatus,
                        CleanupStatusChanged = assignedRecord.CleanupStatusChanged,
                    };

                    return result;
                });
        }

        /// <inheritdoc/>
        public Task<bool> ProcessHeartbeatAsync(Guid id, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            return logger.RetryOperationScopeAsync(
                $"{LogBaseName}_processheartbeat",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("ResourceId", id);

                    var exists = false;

                    // Try to get result
                    var result = await ResourceRepository.GetAsync(id.ToString(), childLogger.NewChildLogger());
                    if (result != null)
                    {
                        // Set keep alives
                        result.KeepAlives.EnvironmentAlive = DateTime.UtcNow;

                        // Update record
                        await ResourceRepository.UpdateAsync(result, childLogger.NewChildLogger());

                        // If null or is deleted then it doesn't exist
                        exists = result?.IsDeleted == false;
                    }

                    childLogger.FluentAddValue("ResourceExists", exists)
                        .FluentAddValue("ResourceIsNotNull", result != null)
                        .FluentAddValue("ResourceIsDeleted", result != null ? (bool?)result.IsDeleted : null);

                    return exists;
                });
        }

        private Task<bool> UpdateSystemAsync(Guid computeId, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_updatesystem",
                async (childLogger) =>
                {
                    var record = await ResourceRepository.GetAsync(computeId.ToString(), childLogger.NewChildLogger());
                    var queueComponent = record.Components?.Items?.SingleOrDefault(x => x.Value.ComponentType == ResourceType.InputQueue).Value;

                    var result = await QueueProvider.PushMessageAsync(
                        queueComponent.AzureResourceInfo,
                        computeId.GenerateUpdateSystemPayload(),
                        logger.NewChildLogger());

                    return result.Status == Common.Continuation.OperationState.Succeeded;
                });
        }
    }
}