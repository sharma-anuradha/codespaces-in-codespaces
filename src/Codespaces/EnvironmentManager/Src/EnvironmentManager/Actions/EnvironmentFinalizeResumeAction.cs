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
    public class EnvironmentFinalizeResumeAction : EnvironmentBaseFinalizeStartAction<EnvironmentFinalizeResumeActionInput>, IEnvironmentFinalizeResumeAction
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
            : base(environmentStateManager, repository, currentLocationProvider, currentUserProvider, controlPlaneInfo, environmentAccessManager, skuCatalog, skuUtils, resourceBrokerClient)
        {
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "environment_finalize_resume_action";

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerClient { get; }

        /// <inheritdoc/>
        public Task<CloudEnvironment> RunAsync(
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

            return RunAsync(input, logger);
        }

        /// <inheritdoc/>
        protected override Task<CloudEnvironment> RunCoreAsync(
            EnvironmentFinalizeResumeActionInput input,
            object transientState,
            IDiagnosticsLogger logger)
        {
            ValidateInput(input);

            return ConfigureRunCoreAsync(input, transientState, logger);
        }

        private void ValidateInput(EnvironmentFinalizeResumeActionInput input)
        {
            ValidationUtil.IsTrue(input.Id != default, "Environment Id is required");
            ValidationUtil.IsTrue(input.StorageResourceId != default, "StorageResourceId is required");
        }
    }
}
