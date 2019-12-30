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
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
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
                IOptions<DocumentDbCollectionOptions> options,
                IDocumentDbClientProvider clientProvider,
                IHealthProvider healthProvider,
                IDiagnosticsLoggerFactory loggerFactory,
                LogValueSet defaultLogValues)
            : base(
                  new DocumentDbCollectionOptionsSnapshot(options, ConfigureOptions),
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
            var query = new SqlQuerySpec(
                @"SELECT TOP @count VALUE c
                FROM c
                WHERE c.poolReference.code = @poolCode
                    AND (
                           c.provisioningStatus = @operationStateFailed
                        OR c.provisioningStatus = @operationStateCancelled
                        OR c.startingStatus = @operationStateFailed
                        OR c.startingStatus = @operationStateCancelled
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
                        )
                        OR (
                            (c.deletingStatus = @operationStateInitialized
                             OR c.deletingStatus = @operationStateInProgress
                            ) AND c.deletingStatusChanged <= @operationFailedTimeLimit
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
                    new SqlParameter { Name = "@operationFailedTimeLimit", Value = DateTime.UtcNow.AddHours(-1) },
                });

            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<ResourceRecord>(uri, query, feedOptions).AsDocumentQuery(), logger);

            return items;
        }
    }
}
