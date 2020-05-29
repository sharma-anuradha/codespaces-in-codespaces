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
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
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
            ISecretManager secretManager)
        {
            ResourceRepository = Requires.NotNull(resourceRepository, nameof(resourceRepository));
            ResourceContinuationOperations = Requires.NotNull(resourceContinuationOperations, nameof(resourceContinuationOperations));
            AllocationStrategies = Requires.NotNull(allocationStrategies, nameof(allocationStrategies));
            SecretManager = Requires.NotNull(secretManager, nameof(secretManager));
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

        /// <inheritdoc/>
        public Task<IEnumerable<AllocateResult>> AllocateAsync(
            Guid environmentId, IEnumerable<AllocateInput> inputs, string trigger, IDiagnosticsLogger logger)
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

                    return await allocationHandler.AllocateAsync(environmentId, inputs, trigger, logger);
                });
        }

        /// <inheritdoc/>
        public Task<AllocateResult> AllocateAsync(
            Guid environmentId, AllocateInput input, string trigger, IDiagnosticsLogger logger)
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

                    return await allocationHandler.AllocateAsync(environmentId, input, trigger, logger);
                });
        }

        /// <inheritdoc/>
        public Task<bool> StartAsync(
            Guid environmentId, StartAction action, IEnumerable<StartInput> resources, string trigger, IDiagnosticsLogger logger)
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
                        backingResources.Add((Resource: resource, Record: record));
                    }

                    // Switch between different actions
                    switch (action)
                    {
                        case StartAction.StartCompute:

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
                                if (computeResource.Resource.FilterSecrets?.PrioritizedSecretStoreResources != null &&
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
                                    trigger,
                                    childLogger.NewChildLogger());
                            }
                            else
                            {
                                throw new NotSupportedException($"Start compute action expects 2 resource and {resources.Count()} was supplied.");
                            }

                            break;
                        case StartAction.StartArchive:
                            if (backingResources.Count == 2)
                            {
                                // Select target resorces
                                var blobResource = backingResources.Single(x => x.Record.Type == ResourceType.StorageArchive);
                                var storageResource = backingResources.Single(x => x.Record.Type == ResourceType.StorageFileShare);

                                childLogger.FluentAddBaseValue("StorageResourceId", storageResource.Resource.ResourceId)
                                    .FluentAddBaseValue("ArchiveStorageResourceId", blobResource.Resource.ResourceId);

                                await ResourceContinuationOperations.StartArchiveAsync(
                                    environmentId,
                                    blobResource.Resource.ResourceId,
                                    storageResource.Resource.ResourceId,
                                    trigger,
                                    childLogger.NewChildLogger());
                            }
                            else
                            {
                                throw new NotSupportedException($"Archive stroage action expects 2 resource and {resources.Count()} was supplied.");
                            }

                            break;
                    }

                    return true;
                });
        }

        /// <inheritdoc/>
        public Task<bool> StartAsync(
            Guid environmentId, StartAction action, StartInput input, string trigger, IDiagnosticsLogger logger)
        {
            throw new NotSupportedException("No action type supports the starting of a single resource.");
        }

        /// <inheritdoc/>
        public Task<bool> SuspendAsync(
            Guid environmentId, IEnumerable<SuspendInput> inputs, string trigger, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_suspend_set",
                async (childLogger) =>
                {
                    var results = await Task.WhenAll(
                        inputs.Select(input => SuspendAsync(environmentId, input, trigger, childLogger.NewChildLogger())));

                    return results.All(x => x);
                });
        }

        /// <inheritdoc/>
        public Task<bool> SuspendAsync(
            Guid environmentId, SuspendInput input, string trigger, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_suspend",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, input.ResourceId);

                    await ResourceContinuationOperations.SuspendAsync(
                        environmentId, input.ResourceId, trigger, logger);

                    return true;
                });
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(
            Guid environmentId, IEnumerable<DeleteInput> inputs, string trigger, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_delete_set",
                async (childLogger) =>
                {
                    var results = await Task.WhenAll(
                        inputs.Select(input => DeleteAsync(environmentId, input, trigger, childLogger.NewChildLogger())));

                    return results.All(x => x);
                });
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(
            Guid environmentId, DeleteInput input, string trigger, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_delete",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, input.ResourceId);

                    await ResourceContinuationOperations.DeleteAsync(
                        environmentId, input.ResourceId, trigger, childLogger.NewChildLogger());

                    return true;
                });
        }

        /// <inheritdoc/>
        public Task<IEnumerable<StatusResult>> StatusAsync(
            Guid environmentId, IEnumerable<StatusInput> inputs, string trigger, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_status_set",
                async (childLogger) =>
                {
                    var results = await Task.WhenAll(
                        inputs.Select(input => StatusAsync(environmentId, input, trigger, childLogger.NewChildLogger())));

                    return results.AsEnumerable();
                });
        }

        /// <inheritdoc/>
        public Task<StatusResult> StatusAsync(
            Guid environmentId, StatusInput input, string trigger, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_status",
                async (childLogger) =>
                {
                    // Get record from db
                    var record = await ResourceRepository.GetAsync(input.ResourceId.ToString(), logger.NewChildLogger());
                    var recordDetails = record.GetDetails();

                    // Build result
                    var result = new StatusResult()
                    {
                        ResourceId = Guid.Parse(record.Id),
                        SkuName = recordDetails.SkuName,
                        Location = recordDetails.Location,
                        Type = record.Type,
                        IsReady = record.IsReady,
                        ProvisioningStatus = record.ProvisioningStatus,
                        ProvisioningStatusChanged = record.ProvisioningStatusChanged,
                        StartingStatus = record.StartingStatus,
                        StartingStatusChanged = record.StartingStatusChanged,
                        DeletingStatus = record.DeletingStatus,
                        DeletingStatusChanged = record.DeletingStatusChanged,
                        CleanupStatus = record.CleanupStatus,
                        CleanupStatusChanged = record.CleanupStatusChanged,
                    };

                    return result;
                });
        }

        /// <inheritdoc/>
        public Task<bool> ProcessHeartbeatAsync(Guid id, string trigger, IDiagnosticsLogger logger)
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
    }
}