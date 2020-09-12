// <copyright file="CosmosDbResourceRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.AzureCosmosDb
{
    /// <summary>
    /// A document repository of <see cref="CosmosDbResourceRepository"/>.
    /// </summary>
    [DocumentDbCollectionId(CollectionName)]
    public partial class CosmosDbResourceRepository : DocumentDbCollection<ResourceRecord>, IResourceRepository
    {
        /// <summary>
        /// The name of the collection in CosmosDB.
        /// </summary>
        public const string CollectionName = "resources";

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbResourceRepository"/> class.
        /// </summary>
        /// <param name="options">The collection options snapshot.</param>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public CosmosDbResourceRepository(
                IOptionsMonitor<DocumentDbCollectionOptions> options,
                IDocumentDbClientProvider clientProvider,
                IHealthProvider healthProvider,
                IDiagnosticsLoggerFactory loggerFactory,
                LogValueSet defaultLogValues)
            : base(
                  options,
                  clientProvider,
                  healthProvider,
                  loggerFactory,
                  defaultLogValues)
        {
        }

        /// <summary>
        /// Configures the standard options for this repository.
        /// </summary>
        /// <param name="options">The options instance.</param>
        public static void ConfigureOptions(DocumentDbCollectionOptions options)
        {
            Requires.NotNull(options, nameof(options));
            options.PartitioningStrategy = PartitioningStrategy.IdOnly;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetPoolCodesForUnassignedAsync(IDiagnosticsLogger logger)
        {
            var query = new SqlQuerySpec(
                @"SELECT DISTINCT VALUE c.poolReference.code 
                FROM c
                WHERE c.isAssigned = @isAssigned
                    AND c.isDeleted = @isDeleted",
                new SqlParameterCollection
                {
                    new SqlParameter { Name = "@isAssigned", Value = false },
                    new SqlParameter { Name = "@isDeleted", Value = false },
                });

            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<string>(uri, query, feedOptions).AsDocumentQuery(), logger);

            return items;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetAllPoolQueueCodesAsync(IDiagnosticsLogger logger)
        {
            var query = new SqlQuerySpec(
                @"SELECT DISTINCT VALUE c.poolReference.code 
                FROM c
                WHERE c.type = @type",
                new SqlParameterCollection
                {
                   new SqlParameter { Name = "@type", Value = ResourceType.PoolQueue.ToString() },
                });

            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<string>(uri, query, feedOptions).AsDocumentQuery(), logger);

            return items;
        }

        /// <inheritdoc/>
        public async Task<ResourceRecord> GetPoolReadyUnassignedAsync(string poolCode, IDiagnosticsLogger logger)
        {
            var query = new SqlQuerySpec(
                @"SELECT TOP 1 *
                FROM c
                WHERE c.poolReference.code = @poolCode
                    AND c.isAssigned = @isAssigned
                    AND c.isReady = @isReady
                    AND c.isDeleted = @isDeleted",
                new SqlParameterCollection
                {
                    new SqlParameter { Name = "@poolCode", Value = poolCode },
                    new SqlParameter { Name = "@isAssigned", Value = false },
                    new SqlParameter { Name = "@isReady", Value = true },
                    new SqlParameter { Name = "@isDeleted", Value = false },
                });

            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<ResourceRecord>(uri, query, feedOptions).AsDocumentQuery(), logger);

            var result = items.FirstOrDefault();

            return result;
        }

        /// <inheritdoc/>
        public async Task<int> GetPoolUnassignedCountAsync(string poolCode, IDiagnosticsLogger logger)
        {
            var query = new SqlQuerySpec(
                @"SELECT VALUE COUNT(1)
                FROM c
                WHERE c.poolReference.code = @poolCode
                    AND c.isAssigned = @isAssigned
                    AND c.isDeleted = @isDeleted
                    AND c.provisioningStatus != @provisioningStatusFailed
                    AND c.provisioningStatus != @provisioningStatusCancelled",
                new SqlParameterCollection
                {
                    new SqlParameter { Name = "@poolCode", Value = poolCode },
                    new SqlParameter { Name = "@isAssigned", Value = false },
                    new SqlParameter { Name = "@isDeleted", Value = false },
                    new SqlParameter { Name = "@provisioningStatusFailed", Value = OperationState.Failed.ToString() },
                    new SqlParameter { Name = "@provisioningStatusCancelled", Value = OperationState.Cancelled.ToString() },
                });

            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<int>(uri, query, feedOptions).AsDocumentQuery(), logger);

            var count = items.FirstOrDefault();

            return count;
        }

        /// <inheritdoc/>
        public async Task<int> GetPoolReadyUnassignedCountAsync(string poolCode, IDiagnosticsLogger logger)
        {
            var query = new SqlQuerySpec(
                @"SELECT VALUE COUNT(1)
                FROM c
                WHERE c.poolReference.code = @poolCode
                    AND c.isAssigned = @isAssigned
                    AND c.isReady = @isReady
                    AND c.isDeleted = @isDeleted",
                new SqlParameterCollection
                {
                    new SqlParameter { Name = "@poolCode", Value = poolCode },
                    new SqlParameter { Name = "@isAssigned", Value = false },
                    new SqlParameter { Name = "@isReady", Value = true },
                    new SqlParameter { Name = "@isDeleted", Value = false },
                });

            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<int>(uri, query, feedOptions).AsDocumentQuery(), logger);

            var count = items.FirstOrDefault();

            return count;
        }

        /// <inheritdoc/>
        public async Task<int> GetPoolUnassignedVersionCountAsync(string poolCode, string poolVersionCode, IDiagnosticsLogger logger)
        {
            var query = new SqlQuerySpec(
                @"SELECT VALUE COUNT(1)
                FROM c
                WHERE c.poolReference.code = @poolCode
                    AND c.poolReference.versionCode = @poolVersionCode
                    AND c.isAssigned = @isAssigned
                    AND c.isDeleted = @isDeleted
                    AND c.provisioningStatus != @provisioningStatusFailed
                    AND c.provisioningStatus != @provisioningStatusCancelled",
                new SqlParameterCollection
                {
                    new SqlParameter { Name = "@poolCode", Value = poolCode },
                    new SqlParameter { Name = "@poolVersionCode", Value = poolVersionCode },
                    new SqlParameter { Name = "@isAssigned", Value = false },
                    new SqlParameter { Name = "@isDeleted", Value = false },
                    new SqlParameter { Name = "@provisioningStatusFailed", Value = OperationState.Failed.ToString() },
                    new SqlParameter { Name = "@provisioningStatusCancelled", Value = OperationState.Cancelled.ToString() },
                });

            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<int>(uri, query, feedOptions).AsDocumentQuery(), logger);

            var count = items.FirstOrDefault();

            return count;
        }

        /// <inheritdoc/>
        public async Task<int> GetPoolReadyUnassignedVersionCountAsync(string poolCode, string poolVersionCode, IDiagnosticsLogger logger)
        {
            var query = new SqlQuerySpec(
                @"SELECT VALUE COUNT(1)
                FROM c
                WHERE c.poolReference.code = @poolCode
                    AND c.poolReference.versionCode = @poolVersionCode
                    AND c.isAssigned = @isAssigned
                    AND c.isReady = @isReady
                    AND c.isDeleted = @isDeleted",
                new SqlParameterCollection
                {
                    new SqlParameter { Name = "@poolCode", Value = poolCode },
                    new SqlParameter { Name = "@poolVersionCode", Value = poolVersionCode },
                    new SqlParameter { Name = "@isAssigned", Value = false },
                    new SqlParameter { Name = "@isReady", Value = true },
                    new SqlParameter { Name = "@isDeleted", Value = false },
                });

            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<int>(uri, query, feedOptions).AsDocumentQuery(), logger);

            var count = items.FirstOrDefault();

            return count;
        }

        /// <inheritdoc/>
        public async Task<int> GetPoolUnassignedNotVersionCountAsync(string poolCode, string poolVersionCode, IDiagnosticsLogger logger)
        {
            var query = new SqlQuerySpec(
                @"SELECT VALUE COUNT(1)
                FROM c
                WHERE c.poolReference.code = @poolCode
                    AND c.poolReference.versionCode != @poolVersionCode
                    AND c.isAssigned = @isAssigned
                    AND c.isDeleted = @isDeleted
                    AND c.provisioningStatus != @provisioningStatusFailed
                    AND c.provisioningStatus != @provisioningStatusCancelled",
                new SqlParameterCollection
                {
                    new SqlParameter { Name = "@poolCode", Value = poolCode },
                    new SqlParameter { Name = "@poolVersionCode", Value = poolVersionCode },
                    new SqlParameter { Name = "@isAssigned", Value = false },
                    new SqlParameter { Name = "@isDeleted", Value = false },
                    new SqlParameter { Name = "@provisioningStatusFailed", Value = OperationState.Failed.ToString() },
                    new SqlParameter { Name = "@provisioningStatusCancelled", Value = OperationState.Cancelled.ToString() },
                });

            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<int>(uri, query, feedOptions).AsDocumentQuery(), logger);

            var count = items.FirstOrDefault();

            return count;
        }

        /// <inheritdoc/>
        public async Task<int> GetPoolReadyUnassignedNotVersionCountAsync(string poolCode, string poolVersionCode, IDiagnosticsLogger logger)
        {
            var query = new SqlQuerySpec(
                @"SELECT VALUE COUNT(1)
                FROM c
                WHERE c.poolReference.code = @poolCode
                    AND c.poolReference.versionCode != @poolVersionCode
                    AND c.isAssigned = @isAssigned
                    AND c.isReady = @isReady
                    AND c.isDeleted = @isDeleted",
                new SqlParameterCollection
                {
                    new SqlParameter { Name = "@poolCode", Value = poolCode },
                    new SqlParameter { Name = "@poolVersionCode", Value = poolVersionCode },
                    new SqlParameter { Name = "@isAssigned", Value = false },
                    new SqlParameter { Name = "@isReady", Value = true },
                    new SqlParameter { Name = "@isDeleted", Value = false },
                });

            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<int>(uri, query, feedOptions).AsDocumentQuery(), logger);

            var count = items.FirstOrDefault();

            return count;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetPoolUnassignedAsync(string poolCode, int count, IDiagnosticsLogger logger)
        {
            var query = new SqlQuerySpec(
                @"SELECT TOP @count VALUE c.id
                FROM c
                WHERE c.poolReference.code = @poolCode
                    AND c.isAssigned = @isAssigned
                    AND c.isDeleted = @isDeleted
                    AND c.provisioningStatus != @provisioningStatusFailed
                    AND c.provisioningStatus != @provisioningStatusCancelled",
                new SqlParameterCollection
                {
                    new SqlParameter { Name = "@count", Value = count },
                    new SqlParameter { Name = "@poolCode", Value = poolCode },
                    new SqlParameter { Name = "@isAssigned", Value = false },
                    new SqlParameter { Name = "@isDeleted", Value = false },
                    new SqlParameter { Name = "@provisioningStatusFailed", Value = OperationState.Failed.ToString() },
                    new SqlParameter { Name = "@provisioningStatusCancelled", Value = OperationState.Cancelled.ToString() },
                });

            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<string>(uri, query, feedOptions).AsDocumentQuery(), logger);

            return items;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetPoolUnassignedNotVersionAsync(string poolCode, string poolVersionCode, int count, IDiagnosticsLogger logger)
        {
            var query = new SqlQuerySpec(
                @"SELECT TOP @count VALUE c.id
                FROM c
                WHERE c.poolReference.code = @poolCode
                    AND c.poolReference.versionCode != @poolVersionCode
                    AND c.isAssigned = @isAssigned
                    AND c.isDeleted = @isDeleted
                    AND c.provisioningStatus != @provisioningStatusFailed
                    AND c.provisioningStatus != @provisioningStatusCancelled",
                new SqlParameterCollection
                {
                    new SqlParameter { Name = "@count", Value = count },
                    new SqlParameter { Name = "@poolCode", Value = poolCode },
                    new SqlParameter { Name = "@poolVersionCode", Value = poolVersionCode },
                    new SqlParameter { Name = "@isAssigned", Value = false },
                    new SqlParameter { Name = "@isDeleted", Value = false },
                    new SqlParameter { Name = "@provisioningStatusFailed", Value = OperationState.Failed.ToString() },
                    new SqlParameter { Name = "@provisioningStatusCancelled", Value = OperationState.Cancelled.ToString() },
                });

            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<string>(uri, query, feedOptions).AsDocumentQuery(), logger);

            return items;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ResourceRecord>> GetFailedOperationAsync(string poolCode, int count, IDiagnosticsLogger logger)
        {
            // Look for failed resources, or resources that are stuck in a temporary state for too long.
            // Special case: For resources that are stuck/failed in the "Starting" state, only consider
            // the VM resource type. Storage resources should not be considered because they contain user
            // data that we do not want to clean up until the user has explicitly asked for deletion.
            //
            // Also, for VMs, check instances where heartbeat was never received (which means:
            // * isAssigned = false
            // * isReady = false
            // * created <= 1 hour ago
            var query = new SqlQuerySpec(
                @"SELECT TOP @count VALUE c
                FROM c
                WHERE c.poolReference.code = @poolCode
                    AND (
                           c.provisioningStatus = @operationStateFailed
                        OR c.provisioningStatus = @operationStateCancelled
                        OR (
                             (c.startingStatus = @operationStateFailed
                             OR c.startingStatus = @operationStateCancelled
                             ) AND c.type = @computeVmResourceType
                        )
                        OR c.deletingStatus = @operationStateFailed
                        OR c.deletingStatus = @operationStateCancelled
                        OR (
                            (c.provisioningStatus = @operationStateInitialized
                             OR c.provisioningStatus = @operationStateInProgress
                            ) AND c.provisioningStatusChanged <= @operationFailedTimeLimit
                        )
                        OR (
                            c.provisioningStatus = null
                            AND c.created <= @operationFailedTimeLimit
                        )
                        OR (
                            (c.startingStatus = @operationStateInitialized
                            OR c.startingStatus = @operationStateInProgress
                            ) AND c.startingStatusChanged <= @operationFailedTimeLimit
                            AND c.type = @computeVmResourceType
                        )
                        OR (
                            (c.deletingStatus = @operationStateInitialized
                             OR c.deletingStatus = @operationStateInProgress
                            ) AND c.deletingStatusChanged <= @operationFailedTimeLimit
                        )
                        OR (
                            c.type = @computeVmResourceType
                            AND c.provisioningStatus = @operationStateSucceeded
                            AND c.isAssigned = false
                            AND c.isReady = false
                            AND c.created <= @operationFailedTimeLimit
                        )
                    )",
                new SqlParameterCollection
                {
                    new SqlParameter { Name = "@count", Value = count },
                    new SqlParameter { Name = "@poolCode", Value = poolCode },
                    new SqlParameter { Name = "@operationStateFailed", Value = OperationState.Failed.ToString() },
                    new SqlParameter { Name = "@operationStateCancelled", Value = OperationState.Cancelled.ToString() },
                    new SqlParameter { Name = "@operationStateInitialized", Value = OperationState.Initialized.ToString() },
                    new SqlParameter { Name = "@operationStateInProgress", Value = OperationState.InProgress.ToString() },
                    new SqlParameter { Name = "@operationStateSucceeded", Value = OperationState.Succeeded.ToString() },
                    new SqlParameter { Name = "@operationFailedTimeLimit", Value = DateTime.UtcNow.AddHours(-1) },
                    new SqlParameter { Name = "@computeVmResourceType", Value = ResourceType.ComputeVM.ToString() },
                });

            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<ResourceRecord>(uri, query, feedOptions).AsDocumentQuery(), logger);

            return items;
        }

        /// <inheritdoc/>
        public async Task<ResourceRecord> GetPoolQueueRecordAsync(string poolCode, IDiagnosticsLogger logger)
        {
            var query = new SqlQuerySpec(
                @"SELECT TOP 1 *
                FROM c
                WHERE c.poolReference.code = @poolCode
                    AND c.type = @type",
                new SqlParameterCollection
                {
                    new SqlParameter { Name = "@poolCode", Value = poolCode },
                    new SqlParameter { Name = "@type", Value = ResourceType.PoolQueue.ToString() },
                });

            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<ResourceRecord>(uri, query, feedOptions).AsDocumentQuery(), logger);

            var result = items.FirstOrDefault();

            return result;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<SystemResourceCountByDimensions>> GetResourceCountByDimensionsAsync(IDiagnosticsLogger logger)
        {
            var query = new SqlQuerySpec(
                @"SELECT value {
                    count: d.count, 
                    type: d.type, 
                    skuName: d.skuName, 
                    location: d.location, 
                    isReady: d.isReady, 
                    isAssigned: d.isAssigned, 
                    poolReferenceCode: d.code, 
                    subscriptionId: d.subscriptionId
                } FROM (
                    SELECT 
                        COUNT(1) AS count, 
                        c.type, c.skuName, 
                        c.location, 
                        c.isReady, 
                        c.isAssigned, 
                        c.poolReference.code,
                        c.azureResourceInfo.subscriptionId
                    FROM c
                    GROUP BY 
                        c.type, 
                        c.skuName,
                        c.location, 
                        c.isReady, 
                        c.isAssigned, 
                        c.poolReference.code, 
                        c.azureResourceInfo.subscriptionId
                ) as d");

            var queryResults = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<SystemResourceCountByDimensions>(uri, query, feedOptions).AsDocumentQuery(), logger);

            return queryResults;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<SystemResourceCountByDimensions>> GetComponentCountByDimensionsAsync(IDiagnosticsLogger logger)
        {
            var countsByDimensions = new Dictionary<SystemResourceDimensions, int>();

            void IncrementCount(SystemResourceDimensions dimensions)
            {
                var updatedValue = countsByDimensions.GetValueOrDefault(dimensions, 0) + 1;
                countsByDimensions[dimensions] = updatedValue;
            }

            void CountComponent(ResourceRecord parent, ResourceComponent component)
            {
                switch (component.ComponentType)
                {
                    case ResourceType.NetworkInterface:
                        {
                            Enum.TryParse(parent.Location, true, out AzureLocation location);
                            var subscriptionId = component.AzureResourceInfo.SubscriptionId.ToString();
                            var poolReferenceCode = parent?.PoolReference?.Code;

                            IncrementCount(new SystemResourceDimensions
                            {
                                Type = ResourceType.NetworkInterface,
                                SkuName = "NetworkInterfaces",
                                SubscriptionId = subscriptionId,
                                IsAssigned = parent.IsAssigned,
                                IsReady = parent.IsReady,
                                Location = location,
                                PoolReferenceCode = parent.PoolReference.Code,
                            });

                            var properties = new AzureResourceInfoNetworkInterfaceProperties(component.AzureResourceInfo.Properties);
                            if (!properties.IsVNetInjected)
                            {
                                if (!string.IsNullOrWhiteSpace(properties.VNet))
                                {
                                    IncrementCount(new SystemResourceDimensions
                                    {
                                        Type = ResourceType.VirtualNetwork,
                                        SkuName = "VirtualNetworks",
                                        SubscriptionId = subscriptionId,
                                        IsAssigned = parent.IsAssigned,
                                        IsReady = parent.IsReady,
                                        Location = location,
                                        PoolReferenceCode = poolReferenceCode,
                                    });
                                }

                                if (!string.IsNullOrWhiteSpace(properties.Nsg))
                                {
                                    IncrementCount(new SystemResourceDimensions
                                    {
                                        Type = ResourceType.NetworkSecurityGroup,
                                        SkuName = "NetworkSecurityGroups",
                                        SubscriptionId = subscriptionId,
                                        IsAssigned = parent.IsAssigned,
                                        IsReady = parent.IsReady,
                                        Location = location,
                                        PoolReferenceCode = poolReferenceCode,
                                    });
                                }
                            }

                            break;
                        }

                    case ResourceType.OSDisk: // Tracked as a full resource, avoid duplicate numbers
                    case ResourceType.InputQueue: // Not tracked, it's a control plane resource
                    default:
                        break;
                }
            }

            await ForEachAsync(
                (ResourceRecord resource) =>
                    resource.Components != null,
                logger,
                (resource, innerLogger) =>
                {
                    foreach (var component in resource.Components.Items.Values)
                    {
                        CountComponent(resource, component);
                    }

                    return Task.CompletedTask;
                });

            return countsByDimensions.Select((kvp) => new SystemResourceCountByDimensions
            {
                Count = kvp.Value,
                Type = kvp.Key.Type,
                SkuName = kvp.Key.SkuName,
                SubscriptionId = kvp.Key.SubscriptionId,
                IsAssigned = kvp.Key.IsAssigned,
                IsReady = kvp.Key.IsReady,
                Location = kvp.Key.Location,
                PoolReferenceCode = kvp.Key.PoolReferenceCode,
            });
        }
    }
}
