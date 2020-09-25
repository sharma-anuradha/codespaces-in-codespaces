// <copyright file="DocumentDbCloudEnvironmentRepository.cs" company="Microsoft">
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
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repository
{
    /// <summary>
    /// A document repository of <see cref="CloudEnvironment"/>.
    /// </summary>
    [DocumentDbCollectionId(CloudEnvironmentsCollectionId)]
    public class DocumentDbCloudEnvironmentRepository
        : DocumentDbCollection<CloudEnvironment>, IGlobalCloudEnvironmentRepository
    {
        /// <summary>
        /// The models collection id.
        /// </summary>
        public const string CloudEnvironmentsCollectionId = "cloud_environments";

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentDbCloudEnvironmentRepository"/> class.
        /// </summary>
        /// <param name="options">The collection options snapshot.</param>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="controlPlaneInfo">The control-plane information.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The diagnostics logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        /// <param name="environmentManagerSettings">The Environment Manager Settings.</param>
        public DocumentDbCloudEnvironmentRepository(
                IOptionsMonitor<DocumentDbCollectionOptions> options,
                IDocumentDbClientProvider clientProvider,
                IControlPlaneInfo controlPlaneInfo,
                IHealthProvider healthProvider,
                IDiagnosticsLoggerFactory loggerFactory,
                LogValueSet defaultLogValues,
                EnvironmentManagerSettings environmentManagerSettings)
            : base(
                  options,
                  clientProvider,
                  healthProvider,
                  loggerFactory,
                  defaultLogValues)
        {
            ControlPlaneLocation = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo)).Stamp.Location;
            EnvironmentManagerSettings = Requires.NotNull(environmentManagerSettings, nameof(environmentManagerSettings));
        }

        /// <inheritdoc/>
        public AzureLocation ControlPlaneLocation { get; }

        private EnvironmentManagerSettings EnvironmentManagerSettings { get; }

        /// <summary>
        /// Configures the standard options for this repository.
        /// </summary>
        /// <param name="options">The options instance.</param>
        /// <remarks>
        /// Keep this in sync with <see cref="CloudEnvironmentCosmosContainer.ConfigureOptions"/>.
        /// </remarks>
        public static void ConfigureOptions(DocumentDbCollectionOptions options)
        {
            Requires.NotNull(options, nameof(options));
            options.PartitioningStrategy = PartitioningStrategy.IdOnly;
        }

        /// <inheritdoc/>
        public override Task<CloudEnvironment> CreateOrUpdateAsync(
            [ValidatedNotNull] CloudEnvironment document,
            [ValidatedNotNull] IDiagnosticsLogger logger)
        {
            Requires.NotNull(document, nameof(document));
            Requires.NotNull(logger, nameof(logger));

            // TODO: ADD option to SDK
            // bool AutoUpdateTimeStamps { get; set; }
            document.Updated = DateTime.UtcNow;

            return base.CreateOrUpdateAsync(document, logger);
        }

        /// <inheritdoc/>
        /// <summary>
        /// Updates the model document in the database. The document's `Updated` field is also set to UTC now.
        /// </summary>
        public override Task<CloudEnvironment> UpdateAsync(
            [ValidatedNotNull] CloudEnvironment document,
            [ValidatedNotNull] IDiagnosticsLogger logger)
        {
            Requires.NotNull(document, nameof(document));
            Requires.NotNull(logger, nameof(logger));

            // TODO: ADD option to SDK
            // bool AutoUpdateTimeStamps { get; set; }
            document.Updated = DateTime.UtcNow;

            return base.UpdateAsync(document, logger);
        }

        /// <inheritdoc/>
        public async Task<int> GetCloudEnvironmentSubscriptionCountAsync(
            IDiagnosticsLogger logger)
        {
            // c.planID is a fully qualified Azure resource path. The values substringed below extract the subscription field. A future suggestion could be to always log the subscriptionID on the Cloud Environment to make it easier to query for this.
            // FIXME: Once we migrate cloud environments to a regional DB, we won't need the control-plane location filtering logic.
            var query = new SqlQuerySpec(
                @"SELECT VALUE SUM(1)
                FROM (
                    SELECT DISTINCT VALUE SUBSTRING(c.planId,15,36)
                    FROM c
                    WHERE (((
                        IS_DEFINED(c.controlPlaneLocation) = false
                            OR c.controlPlaneLocation = null) AND c.location = @controlPlaneLocation)
                        OR c.controlPlaneLocation = @controlPlaneLocation)) d",
                new SqlParameterCollection
                {
                    new SqlParameter { Name = "@controlPlaneLocation", Value = ControlPlaneLocation.ToString() },
                });

            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<int>(uri, query, feedOptions).AsDocumentQuery(), logger);
            var count = items.FirstOrDefault();
            return count;
        }

        /// <inheritdoc/>
        public async Task<int> GetCloudEnvironmentPlanCountAsync(
            IDiagnosticsLogger logger)
        {
            // FIXME: Once we migrate cloud environments to a regional DB, we won't need the control-plane location filtering logic.
            var query = new SqlQuerySpec(
                @"SELECT VALUE SUM(1)
                FROM (
                    SELECT DISTINCT VALUE c.planId
                    FROM c
                    WHERE(((
                        IS_DEFINED(c.controlPlaneLocation) = false
                            OR c.controlPlaneLocation = null) AND c.location = @controlPlaneLocation)
                        OR c.controlPlaneLocation = @controlPlaneLocation)) d",
                new SqlParameterCollection
                {
                    new SqlParameter { Name = "@controlPlaneLocation", Value = ControlPlaneLocation.ToString() },
                });

            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<int>(uri, query, feedOptions).AsDocumentQuery(), logger);
            var count = items.FirstOrDefault();
            return count;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> GetFailedOperationAsync(
            string idShard,
            int count,
            IDiagnosticsLogger logger)
        {
            // Look for failed resources, or resources that are stuck in a temporary state for too long.
            // Special case: For resources that are stuck/failed in the "Starting" state, only consider
            // the VM resource type. Storage resources should not be considered because they contain user
            // data that we do not want to clean up until the user has explicitly asked for deletion.
            // FIXME: Once we migrate cloud environments to a regional DB, we won't need the control-plane location filtering logic.
            var query = new SqlQuerySpec(
                @"SELECT TOP @count VALUE c
                FROM c
                WHERE STARTSWITH(c.id, @idShard)
                    AND (((
                        IS_DEFINED(c.controlPlaneLocation) = false
                            OR c.controlPlaneLocation = null) AND c.location = @controlPlaneLocation)
                        OR c.controlPlaneLocation = @controlPlaneLocation)
                    AND (
                        c.transitions.archiving.status = @operationStateFailed
                        OR c.transitions.archiving.status = @operationStateCancelled
                        OR (
                            (c.transitions.archiving.status = @operationStateInitialized
                                OR c.transitions.archiving.status = @operationStateInProgress
                            ) AND c.transitions.archiving.statusChanged <= @operationFailedTimeLimit
                        )
                    )",
                new SqlParameterCollection
                {
                    new SqlParameter { Name = "@count", Value = count },
                    new SqlParameter { Name = "@idShard", Value = idShard },
                    new SqlParameter { Name = "@operationStateFailed", Value = OperationState.Failed.ToString() },
                    new SqlParameter { Name = "@operationStateCancelled", Value = OperationState.Cancelled.ToString() },
                    new SqlParameter { Name = "@operationStateInitialized", Value = OperationState.Initialized.ToString() },
                    new SqlParameter { Name = "@operationStateInProgress", Value = OperationState.InProgress.ToString() },
                    new SqlParameter { Name = "@operationFailedTimeLimit", Value = DateTime.UtcNow.AddHours(-1.25) },
                    new SqlParameter { Name = "@controlPlaneLocation", Value = ControlPlaneLocation.ToString() },
                });

            var items = await QueryAsync(
                (client, uri, feedOptions) => client.CreateDocumentQuery<CloudEnvironment>(uri, query, feedOptions).AsDocumentQuery(), logger);

            return items;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> GetEnvironmentsReadyForArchiveAsync(
            string idShard,
            int count,
            DateTime shutdownCutoffTime,
            DateTime softDeleteCutoffTime,
            IDiagnosticsLogger logger)
        {
            var queryParams = new SqlParameterCollection
                {
                    new SqlParameter { Name = "@count", Value = count },
                    new SqlParameter { Name = "@idShard", Value = idShard },
                    new SqlParameter { Name = "@stateShutdown", Value = CloudEnvironmentState.Shutdown.ToString() },
                    new SqlParameter { Name = "@shutdownCutoffTime", Value = shutdownCutoffTime },
                    new SqlParameter { Name = "@softDeleteCutoffTime", Value = softDeleteCutoffTime },
                    new SqlParameter { Name = "@controlPlaneLocation", Value = ControlPlaneLocation.ToString() },
                    new SqlParameter { Name = "@attemptCountLimit", Value = 5 },
                };

            // FIXME: Once we migrate cloud environments to a regional DB, we won't need the control-plane location filtering logic.
            var query = new SqlQuerySpec(
                @"SELECT TOP @count VALUE c
                FROM c
                WHERE STARTSWITH(c.id, @idShard)
                    AND (c.storage != null OR c.osDisk != null)
                    AND c.state = @stateShutdown
                    AND ((c.lastStateUpdated < @shutdownCutoffTime) OR 
                        (c.isDeleted = true
                            AND c.lastDeleted < @softDeleteCutoffTime))
                    AND (
                        IS_DEFINED(c.transitions) = false
                        OR (c.transitions.archiving.status = null
                            AND c.transitions.archiving.attemptCount <= @attemptCountLimit))
                    AND (((
                        IS_DEFINED(c.controlPlaneLocation) = false
                            OR c.controlPlaneLocation = null) AND c.location = @controlPlaneLocation)
                        OR c.controlPlaneLocation = @controlPlaneLocation)",
                queryParams);

            if (await EnvironmentManagerSettings.DynamicEnvironmentArchivalTimeEnabled(logger.NewChildLogger()))
            {
                query = new SqlQuerySpec(
                    @"SELECT TOP @count VALUE c
                    FROM c
                    WHERE STARTSWITH(c.id, @idShard)
                        AND (c.storage != null OR c.osDisk != null)
                        AND c.state = @stateShutdown
                        AND (
                            c.scheduledArchival < GetCurrentDateTime()
                            OR c.lastStateUpdated < @shutdownCutoffTime
                            OR (c.isDeleted = true
                                AND c.lastDeleted < @softDeleteCutoffTime))
                        AND (
                            IS_DEFINED(c.transitions) = false
                            OR (c.transitions.archiving.status = null
                                AND c.transitions.archiving.attemptCount <= @attemptCountLimit))
                        AND (((
                            IS_DEFINED(c.controlPlaneLocation) = false
                                OR c.controlPlaneLocation = null) AND c.location = @controlPlaneLocation)
                            OR c.controlPlaneLocation = @controlPlaneLocation)",
                    queryParams);
            }

            var items = await QueryAsync(
               (client, uri, feedOptions) => client.CreateDocumentQuery<CloudEnvironment>(uri, query, feedOptions).AsDocumentQuery(), logger);

            return items;
        }

        /// <inheritdoc/>
        public async Task<int> GetEnvironmentsArchiveJobActiveCountAsync(
            IDiagnosticsLogger logger)
        {
            // FIXME: Once we migrate cloud environments to a regional DB, we won't need the control-plane location filtering logic.
            var query = new SqlQuerySpec(
                @"SELECT VALUE COUNT(1)
                FROM c
                WHERE c.transitions.archiving.status = @activeStatus
                    AND (((
                        IS_DEFINED(c.controlPlaneLocation) = false
                            OR c.controlPlaneLocation = null) AND c.location = @controlPlaneLocation)
                        OR c.controlPlaneLocation = @controlPlaneLocation)",
                new SqlParameterCollection
                {
                    new SqlParameter { Name = "@activeStatus", Value = OperationState.InProgress.ToString() },
                    new SqlParameter { Name = "@controlPlaneLocation", Value = ControlPlaneLocation.ToString() },
                });

            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<int>(uri, query, feedOptions).AsDocumentQuery(), logger);
            var count = items.FirstOrDefault();
            return count;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> GetEnvironmentsReadyForHardDeleteAsync(
            string idShard,
            DateTime cutoffTime,
            IDiagnosticsLogger logger)
        {
            var query = new SqlQuerySpec(
                @"SELECT VALUE c FROM c
                WHERE STARTSWITH(c.id, @idShard)
                    AND (c.isDeleted = true
                         AND c.lastDeleted < @cutoffTime)
                    AND (c.state != @deletedState)
                    AND (c.transitions.archiving.status != @archivingInProgressStatus
                         AND c.transitions.archiving.status != @archivingInitializedStatus
                         AND c.transitions.archiving.status != @archivingTriggeredStatus)
                    AND (((
                        IS_DEFINED(c.controlPlaneLocation) = false
                            OR c.controlPlaneLocation = null) AND c.location = @controlPlaneLocation)
                        OR c.controlPlaneLocation = @controlPlaneLocation)",
                new SqlParameterCollection
                {
                    new SqlParameter { Name = "@idShard", Value = idShard },
                    new SqlParameter { Name = "@deletedState", Value = CloudEnvironmentState.Deleted.ToString() },
                    new SqlParameter { Name = "@archivingInProgressStatus", Value = OperationState.InProgress.ToString() },
                    new SqlParameter { Name = "@archivingInitializedStatus", Value = OperationState.Initialized.ToString() },
                    new SqlParameter { Name = "@archivingTriggeredStatus", Value = OperationState.Triggered.ToString() },
                    new SqlParameter { Name = "@cutoffTime", Value = cutoffTime },
                    new SqlParameter { Name = "@controlPlaneLocation", Value = ControlPlaneLocation.ToString() },
                });

            var items = await QueryAsync(
                (client, uri, feedOptions) => client.CreateDocumentQuery<CloudEnvironment>(uri, query, feedOptions).AsDocumentQuery(), logger);

            return items;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<CloudEnvironment>> GetAllEnvironmentsInSubscriptionAsync(string subscriptionId, IDiagnosticsLogger logger)
        {
            // TODO: Make this query a direct match on a non-existant subscription field.
            // FIXME: should this filter on control-plane location as well?
            var query = new SqlQuerySpec(
                @"SELECT *
                FROM c
                WHERE CONTAINS(c.planId, @subscription)",
                new SqlParameterCollection
                {
                    new SqlParameter { Name = "@subscription", Value = subscriptionId },
                });

            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<CloudEnvironment>(uri, query, feedOptions).AsDocumentQuery(), logger.NewChildLogger());
            return items;
        }

        /// <inheritdoc />
        public async Task<CloudEnvironment> GetEnvironmentUsingResource(string resourceId, ResourceType resourceType, IDiagnosticsLogger logger)
        {
            if (resourceType != ResourceType.ComputeVM &&
                resourceType != ResourceType.StorageFileShare &&
                resourceType != ResourceType.StorageArchive &&
                resourceType != ResourceType.OSDisk &&
                resourceType != ResourceType.Snapshot)
            {
                throw new ArgumentException($"Resource Type {resourceType} is not handled");
            }

            var query = new SqlQuerySpec(
                @"SELECT TOP @count VALUE c
                FROM c
                WHERE
                (
                    (@targetResourceType = @computeVmResourceType and c.compute != null and c.compute.resourceId = @targetResourceId) or
                    (@targetResourceType = @storageFileShareResourceType and c.storage != null and c.storage.resourceId = @targetResourceId) or
                    (@targetResourceType = @storageArchiveResourceType and c.storage != null and c.storage.resourceId = @targetResourceId) or
                    (@targetResourceType = @osDiskResourceType and c.osDisk != null and c.osDisk.resourceId = @targetResourceId) or
                    (@targetResourceType = @snapshotResourceType and c.osDiskSnapshot != null and c.osDiskSnapshot.resourceId = @targetResourceId)
                )",
                new SqlParameterCollection
                {
                    new SqlParameter { Name = "@count", Value = 1 },
                    new SqlParameter { Name = "@targetResourceId", Value = resourceId },
                    new SqlParameter { Name = "@targetResourceType", Value = resourceType },
                    new SqlParameter { Name = "@computeVmResourceType", Value = ResourceType.ComputeVM },
                    new SqlParameter { Name = "@storageFileShareResourceType", Value = ResourceType.StorageFileShare },
                    new SqlParameter { Name = "@storageArchiveResourceType", Value = ResourceType.StorageArchive },
                    new SqlParameter { Name = "@osDiskResourceType", Value = ResourceType.OSDisk },
                    new SqlParameter { Name = "@snapshotResourceType", Value = ResourceType.Snapshot },
                });

            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<CloudEnvironment>(uri, query, feedOptions).AsDocumentQuery(), logger.NewChildLogger());
            return items.FirstOrDefault();
        }
    }
}
