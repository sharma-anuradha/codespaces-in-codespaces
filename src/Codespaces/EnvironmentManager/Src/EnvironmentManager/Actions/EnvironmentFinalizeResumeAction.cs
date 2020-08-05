// <copyright file="EnvironmentFinalizeResumeAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Action to finalize environment resume based on heartbeat.
    /// </summary>
    public class EnvironmentFinalizeResumeAction : EnvironmentItemAction<EnvironmentFinalizeResumeActionInput, object>, IEnvironmentFinalizeResumeAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentFinalizeResumeAction"/> class.
        /// </summary>
        /// <param name="environmentStateManager">Target environment state manager.</param>
        /// <param name="repository">Target repository.</param>
        /// <param name="currentLocationProvider">Target current location provider.</param>
        /// <param name="currentUserProvider">Target current user provider.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        /// <param name="environmentAccessManager">Target environment access manager.</param>
        /// <param name="skuCatalog">Target sku catalog.</param>
        /// <param name="skuUtils">Target skuUtils, to find sku's eligiblity.</param>
        /// <param name="resourceBrokerClient">Target resource broker client.</param>
        public EnvironmentFinalizeResumeAction(
            IEnvironmentStateManager environmentStateManager,
            ICloudEnvironmentRepository repository,
            ICurrentLocationProvider currentLocationProvider,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo,
            IEnvironmentAccessManager environmentAccessManager,
            ISkuCatalog skuCatalog,
            ISkuUtils skuUtils,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerClient)
            : base(environmentStateManager, repository, currentLocationProvider, currentUserProvider, controlPlaneInfo, environmentAccessManager, skuCatalog, skuUtils)
        {
            ResourceBrokerClient = resourceBrokerClient;
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "environment_finalize_resume_action";

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerClient { get; }

        /// <inheritdoc/>
        public async Task<CloudEnvironment> RunAsync(
            Guid environmentId,
            Guid storageResourceId,
            Guid? archiveStorageResourceId,
            IDiagnosticsLogger logger)
        {
            var input = new EnvironmentFinalizeResumeActionInput(environmentId)
            {
                StorageResourceId = storageResourceId,
                ArchiveStorageResourceId = archiveStorageResourceId,
            };

            return await RunAsync(input, logger);
        }

        /// <inheritdoc/>
        protected override async Task<CloudEnvironment> RunCoreAsync(
            EnvironmentFinalizeResumeActionInput input,
            object transientState,
            IDiagnosticsLogger logger)
        {
            ValidateInput(input);

            var record = await FetchAsync(input, logger.NewChildLogger());

            ValidateTargetLocation(record.Value.Location, logger);

            // Detect if environment is archived
            var isEnvironmentArchived = record.Value.Storage.Type == ResourceType.StorageArchive;
            var computeResourceId = record.Value.Compute.ResourceId;

            logger.AddCloudEnvironmentIsArchived(isEnvironmentArchived)
                .AddComputeResourceId(computeResourceId)
                .AddStorageResourceId(input.StorageResourceId)
                .AddArchiveStorageResourceId(input.ArchiveStorageResourceId);

            // Only need to trigger resume callback if environment was archived
            if (isEnvironmentArchived && record.Value.Storage.Type == ResourceType.StorageArchive)
            {
                // Finalize start if we can
                if (input.ArchiveStorageResourceId != null)
                {
                    // Conduct update to swapout archived storage for file storage
                    await logger.RetryOperationScopeAsync(
                        $"{LogBaseName}_resume_callback_update",
                        async (retryLogger) =>
                        {
                            // Fetch resource details
                            var storageDetails = await ResourceBrokerClient.StatusAsync(
                                input.Id,
                                input.StorageResourceId,
                                retryLogger.NewChildLogger());

                            // Switch out storage reference
                            var storageResource = new ResourceAllocationRecord
                            {
                                ResourceId = input.StorageResourceId,
                                Location = storageDetails.Location,
                                SkuName = storageDetails.SkuName,
                                Type = storageDetails.Type,
                                Created = DateTime.UtcNow,
                            };
                            record.PushTransition((environment) =>
                            {
                                environment.Storage = storageResource;
                                environment.Transitions.Archiving.ResetStatus(true);
                            });

                            // Update record
                            await Repository.UpdateTransitionAsync("cloudenvironment", record, logger);
                        });

                    // Delete archive blob once its not needed any more
                    await ResourceBrokerClient.DeleteAsync(
                        input.Id,
                        input.ArchiveStorageResourceId.Value,
                        logger.NewChildLogger());
                }
                else
                {
                    throw new NotSupportedException("Failed to find necessary result and/or supporting data to complete restart.");
                }
            }

            return record.Value;
        }

        private void ValidateInput(EnvironmentFinalizeResumeActionInput input)
        {
            ValidationUtil.IsTrue(input.Id != default, "Environment Id is required");
            ValidationUtil.IsTrue(input.StorageResourceId != default, "StorageResourceId is required");
        }
    }
}
