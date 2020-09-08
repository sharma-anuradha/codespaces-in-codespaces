// <copyright file="ArchiveEnvironmentContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    /// <summary>
    /// Archive Environment Continuation Handler.
    /// </summary>
    public class ArchiveEnvironmentContinuationHandler
        : BaseContinuationTaskMessageHandler<ArchiveEnvironmentContinuationInput>, IArchiveEnvironmentContinuationHandler
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueTarget = "JobArchiveEnvironment";

        /// <summary>
        /// Initializes a new instance of the <see cref="ArchiveEnvironmentContinuationHandler"/> class.
        /// </summary>
        /// <param name="environmentManagerSettings">Environment manager settings.</param>
        /// <param name="environmentStateManager">Target environment manager.</param>
        /// <param name="cloudEnvironmentRepository">Cloud Environment Repository to be used.</param>
        /// <param name="resourceBrokerHttpClient">Target Resource Broker Http Client.</param>
        public ArchiveEnvironmentContinuationHandler(
            EnvironmentManagerSettings environmentManagerSettings,
            IEnvironmentStateManager environmentStateManager,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient)
            : base(cloudEnvironmentRepository)
        {
            EnvironmentManagerSettings = environmentManagerSettings;
            ResourceBrokerHttpClient = resourceBrokerHttpClient;
            EnvironmentStateManager = environmentStateManager;
        }

        /// <inheritdoc/>
        protected override string LogBaseName => EnvironmentLoggingConstants.ContinuationTaskMessageHandlerArchive;

        /// <inheritdoc/>
        protected override string DefaultTarget => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override EnvironmentOperation Operation => EnvironmentOperation.Archiving;

        private EnvironmentManagerSettings EnvironmentManagerSettings { get; }

        private IEnvironmentStateManager EnvironmentStateManager { get; }

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerHttpClient { get; }

        /// <inheritdoc/>
        protected override TransitionState FetchOperationTransition(
            ArchiveEnvironmentContinuationInput input,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            return record.Value.Transitions.Archiving;
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationResult> RunOperationCoreAsync(
            ArchiveEnvironmentContinuationInput operationInput,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("EnvironmentId", operationInput.EnvironmentId)
                .FluentAddValue("ArchiveSubStatus", operationInput.ArchiveStatus)
                .FluentAddValue("ArchiveLastStateUpdated", operationInput.LastStateUpdated)
                .FluentAddValue("ArchiveReason", operationInput.Reason)
                .FluentAddValue("ArchiveResourceId", operationInput?.ArchiveResource?.ResourceId);

            // If the blob is no longer shutdown, then we should fail and cleanup
            if (!operationInput.IsEnvironmentStateValidForArchive(record.Value, logger))
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "StateNoLongerShutdown" };
            }

            // Run operation
            switch (operationInput.ArchiveStatus)
            {
                case ArchiveEnvironmentContinuationInputState.AllocateStorageBlob:
                    // Trigger resource allocation by calling allocate endpoint
                    if (record.Value.OSDisk != default)
                    {
                        // Only archive OS disks if feature is enabled
                        if (await EnvironmentManagerSettings.EnvironmentOSDiskArchiveEnabled(logger))
                        {
                            // Trigger snapshot allocation
                            return await operationInput.RunAllocateStorageSnapshot(ResourceBrokerHttpClient, record, logger);
                        }

                        return new ContinuationResult { Status = OperationState.Succeeded };
                    }

                    return await operationInput.RunAllocateStorageBlob(ResourceBrokerHttpClient, record, logger);
                case ArchiveEnvironmentContinuationInputState.StartStorageBlob:
                    // Trigger blob copy by calling start endpoint
                    return await operationInput.RunStartStorageBlob(ResourceBrokerHttpClient, record, logger);
                case ArchiveEnvironmentContinuationInputState.CheckStartStorageBlob:
                    // Trigger blob copy check by calling start check endpoint
                    return await operationInput.RunCheckArchiveStatus(ResourceBrokerHttpClient, record, logger);
                case ArchiveEnvironmentContinuationInputState.CleanupUnneededStorage:
                    // Trigger storage delete by calling delete endpoint
                    return await operationInput.RunCleanupUnneededResources(CloudEnvironmentRepository, EnvironmentStateManager, ResourceBrokerHttpClient, record, logger, $"{LogBaseName}_status_update");
            }

            return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidArchiveState" };
        }

        /// <inheritdoc/>
        protected override Task<bool> FailOperationShouldTriggerCleanupAsync(
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationResult> FailOperationCleanupCoreAsync(
            ArchiveEnvironmentContinuationInput operationInput,
            EnvironmentRecordRef record,
            string trigger,
            IDiagnosticsLogger logger)
        {
            // If we didn't get as far as switching the record, then we need to delete the allocated resource
            if (((record.Value.Storage != null
                && record.Value.Storage?.Type != ResourceType.StorageArchive)
                || record.Value.OSDisk != null)
                && operationInput.ArchiveResource != null)
            {
                // Make sure we update the archive state to cleanup
                await UpdateRecordAsync(
                    operationInput,
                    record,
                    (environment, innerLogger) =>
                    {
                        return Task.FromResult(environment.Transitions.Archiving.ResetStatus(false));
                    },
                    logger);

                // Trigger delete of resource that we tried to create
                var successful = await ResourceBrokerHttpClient.DeleteAsync(
                    operationInput.EnvironmentId, operationInput.ArchiveResource.ResourceId, logger.NewChildLogger());

                return new ContinuationResult
                {
                    Status = successful ? OperationState.Succeeded : OperationState.Failed,
                };
            }

            return await base.FailOperationCleanupCoreAsync(operationInput, record, trigger, logger);
        }
    }
}
