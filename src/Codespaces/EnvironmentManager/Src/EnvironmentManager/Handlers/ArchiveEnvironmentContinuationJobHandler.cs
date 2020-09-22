// <copyright file="ArchiveEnvironmentContinuationJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    /// <summary>
    /// The archive continuation job handler.
    /// </summary>
    public class ArchiveEnvironmentContinuationJobHandler : EnvironmentContinuationJobHandlerBase<ArchiveEnvironmentContinuationJobHandler.ArchiveContinuationInput, ArchiveEnvironmentContinuationInputState, EnvironmentContinuationResult>
    {
        /// <summary>
        /// Default queue id.
        /// </summary>
        public const string DefaultQueueId = "jobhandler-archive-environment";

        /// <summary>
        /// Initializes a new instance of the <see cref="ArchiveEnvironmentContinuationJobHandler"/> class.
        /// </summary>
        /// <param name="environmentManagerSettings">Environment manager settings.</param>
        /// <param name="environmentStateManager">Target environment manager.</param>
        /// <param name="resourceBrokerHttpClient">Target Resource Broker Http Client.</param>
        /// <param name="cloudEnvironmentRepository">Cloud Environment Repository to be used.</param>
        /// <param name="jobQueueProducerFactory">Thye job queue producer factoryt instance.</param>
        public ArchiveEnvironmentContinuationJobHandler(
            EnvironmentManagerSettings environmentManagerSettings,
            IEnvironmentStateManager environmentStateManager,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IJobQueueProducerFactory jobQueueProducerFactory)
            : base(cloudEnvironmentRepository, jobQueueProducerFactory)
        {
            EnvironmentManagerSettings = environmentManagerSettings;
            ResourceBrokerHttpClient = resourceBrokerHttpClient;
            EnvironmentStateManager = environmentStateManager;
        }

        /// <inheritdoc/>
        public override string QueueId => DefaultQueueId;

        /// <inheritdoc/>
        protected override string LogBaseName => EnvironmentLoggingConstants.ContinuationTaskMessageHandlerArchive;

        /// <inheritdoc/>
        protected override EnvironmentOperation Operation => EnvironmentOperation.Archiving;

        private EnvironmentManagerSettings EnvironmentManagerSettings { get; }

        private IEnvironmentStateManager EnvironmentStateManager { get; }

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerHttpClient { get; }

        /// <inheritdoc/>
        protected override TransitionState FetchOperationTransition(
            ArchiveContinuationInput input,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            return record.Value.Transitions.Archiving;
        }

        /// <inheritdoc/>
        protected override Task<bool> FailOperationShouldTriggerCleanupAsync(
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        protected override async Task FailOperationCleanupCoreAsync(
            ArchiveContinuationInput payload,
            EnvironmentRecordRef record,
            string trigger,
            IDiagnosticsLogger logger)
        {
            // If we didn't get as far as switching the record, then we need to delete the allocated resource
            if (((record.Value.Storage != null
                && record.Value.Storage?.Type != ResourceType.StorageArchive)
                || record.Value.OSDisk != null)
                && payload.ArchiveResource != null)
            {
                // Make sure we update the archive state to cleanup
                await UpdateRecordAsync(
                    payload,
                    record,
                    (environment, innerLogger) =>
                    {
                        return Task.FromResult(environment.Transitions.Archiving.ResetStatus(false));
                    },
                    logger);

                // Trigger delete of resource that we tried to create
                var successful = await ResourceBrokerHttpClient.DeleteAsync(
                    payload.EnvironmentId, payload.ArchiveResource.ResourceId, logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        protected override ArchiveEnvironmentContinuationInputState GetStateFromPayload(ArchiveContinuationInput payload)
        {
            return payload.ArchiveStatus;
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationJobResult<ArchiveEnvironmentContinuationInputState, EnvironmentContinuationResult>> ContinueAsync(
            ArchiveContinuationInput payload,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger,
            CancellationToken cancellationToken)
        {
            // If the blob is no longer shutdown, then we should fail and cleanup
            if (!payload.IsEnvironmentStateValidForArchive(record.Value, logger))
            {
                return ReturnFailed("StateNoLongerShutdown");
            }

            switch (payload.CurrentState)
            {
                case ArchiveEnvironmentContinuationInputState.AllocateStorageBlob:
                    // Trigger resource allocation by calling allocate endpoint
                    if (record.Value.OSDisk != default)
                    {
                        // Only archive OS disks if feature is enabled
                        if (await EnvironmentManagerSettings.EnvironmentOSDiskArchiveEnabled(logger))
                        {
                            // Trigger snapshot allocation
                            return ToContinuationInfo(await payload.RunAllocateStorageSnapshot(ResourceBrokerHttpClient, record, logger), payload);
                        }

                        return ReturnSucceeded(null);
                    }

                    return ToContinuationInfo(await payload.RunAllocateStorageBlob(ResourceBrokerHttpClient, record, logger), payload);
                case ArchiveEnvironmentContinuationInputState.StartStorageBlob:
                    // Trigger blob copy by calling start endpoint
                    return ToContinuationInfo(await payload.RunStartStorageBlob(ResourceBrokerHttpClient, record, logger), payload);
                case ArchiveEnvironmentContinuationInputState.CheckStartStorageBlob:
                    // Trigger blob copy check by calling start check endpoint
                    return ToContinuationInfo(await payload.RunCheckArchiveStatus(ResourceBrokerHttpClient, record, logger), payload);
                case ArchiveEnvironmentContinuationInputState.CleanupUnneededStorage:
                    // Trigger storage delete by calling delete endpoint
                    return ToContinuationInfo(await payload.RunCleanupUnneededResources(CloudEnvironmentRepository, EnvironmentStateManager, ResourceBrokerHttpClient, record, logger, $"{LogBaseName}_status_update"), payload);
            }

            return ReturnFailed("InvalidArchiveState");
        }

        /// <summary>
        /// Continuation input type.
        /// </summary>
        public class ArchiveContinuationInput : EnvironmentContinuationInputBase<ArchiveEnvironmentContinuationInputState>, IArchiveEnvironmentContinuationPayload
        {
            /// <summary>
            /// Gets or sets the Archive State.
            /// </summary>
            [JsonIgnore]
            public ArchiveEnvironmentContinuationInputState ArchiveStatus { get; set; }

            /// <summary>
            /// Gets or sets the Archiuve Resource that exists in the continuation.
            /// </summary>
            public EnvironmentContinuationInputResource ArchiveResource { get; set; }

            /// <summary>
            /// Gets or sets the last state updated time.
            /// </summary>
            public DateTime LastStateUpdated { get; set; }
        }
    }
}
