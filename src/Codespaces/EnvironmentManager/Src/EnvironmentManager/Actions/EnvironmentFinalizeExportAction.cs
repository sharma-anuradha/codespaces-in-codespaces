// <copyright file="EnvironmentFinalizeExportAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Action to finalize environment export based on heartbeat.
    /// </summary>
    public class EnvironmentFinalizeExportAction : EnvironmentBaseFinalizeStartAction<EnvironmentFinalizeExportActionInput>, IEnvironmentFinalizeExportAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentFinalizeExportAction"/> class.
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
        public EnvironmentFinalizeExportAction(
            IEnvironmentStateManager environmentStateManager,
            ICloudEnvironmentRepository repository,
            ICurrentLocationProvider currentLocationProvider,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo,
            IEnvironmentAccessManager environmentAccessManager,
            ISkuCatalog skuCatalog,
            ISkuUtils skuUtils,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerClient)
            : base(environmentStateManager, repository, currentLocationProvider, currentUserProvider, controlPlaneInfo, environmentAccessManager, skuCatalog, skuUtils, resourceBrokerClient)
        {
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "environment_finalize_export_action";

        /// <inheritdoc/>
        public Task<CloudEnvironment> RunAsync(
            Guid environmentId,
            Guid storageResourceId,
            Guid? archiveStorageResourceId,
            string environmentExportBlobUrl,
            IDiagnosticsLogger logger)
        {
            var input = new EnvironmentFinalizeExportActionInput(environmentId)
            {
                StorageResourceId = storageResourceId,
                ArchiveStorageResourceId = archiveStorageResourceId,
                EnvironmentExportBlobUrl = environmentExportBlobUrl,
            };

            return RunAsync(input, logger);
        }

        /// <inheritdoc/>
        protected override async Task<CloudEnvironment> RunCoreAsync(
            EnvironmentFinalizeExportActionInput input,
            object transientState,
            IDiagnosticsLogger logger)
        {
            ValidateInput(input);

            var env = await ConfigureRunCoreAsync(input, transientState, logger);

            // Update export blob url and last state updated for exporting action.
            env.ExportedBlobUrl = input.EnvironmentExportBlobUrl;
            env.LastStateUpdateReason = "Done exporting";
            var updatedEnvironment = await Repository.UpdateAsync(env, logger.NewChildLogger());

            return updatedEnvironment;
        }

        private void ValidateInput(EnvironmentFinalizeExportActionInput input)
        {
            ValidationUtil.IsTrue(input.Id != default, "Environment Id is required");
            ValidationUtil.IsTrue(input.StorageResourceId != default, "StorageResourceId is required");
            ValidationUtil.IsTrue(input.EnvironmentExportBlobUrl != default, "ExportedBlobUrl is required");
        }
    }
}
