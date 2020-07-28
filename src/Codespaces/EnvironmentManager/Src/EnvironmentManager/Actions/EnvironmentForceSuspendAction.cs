// <copyright file="EnvironmentForceSuspendAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Environment force suspend action.
    /// </summary>
    public class EnvironmentForceSuspendAction : EnvironmentItemAction<Guid, object>, IEnvironmentForceSuspendAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentForceSuspendAction"/> class.
        /// </summary>
        /// <param name="environmentStateManager">Target environment state manager.</param>
        /// <param name="repository">Target repository.</param>
        /// <param name="currentLocationProvider">Target current location provider.</param>
        /// <param name="currentUserProvider">Target current user provider.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        /// <param name="environmentAccessManager">Target environment access manager.</param>
        /// <param name="skuCatalog">Target sku catalog.</param>
        /// <param name="skuUtils">Target skuUtils, to find sku's eligiblity.</param>
        /// <param name="environmentContinuation">Target environment continuation.</param>
        /// <param name="resourceBrokerClient">Target resource broker client.</param>
        public EnvironmentForceSuspendAction(
            IEnvironmentStateManager environmentStateManager,
            ICloudEnvironmentRepository repository,
            ICurrentLocationProvider currentLocationProvider,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo,
            IEnvironmentAccessManager environmentAccessManager,
            ISkuCatalog skuCatalog,
            ISkuUtils skuUtils,
            IEnvironmentContinuationOperations environmentContinuation,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerClient)
            : base(environmentStateManager, repository, currentLocationProvider, currentUserProvider, controlPlaneInfo, environmentAccessManager, skuCatalog, skuUtils)
        {
            EnvironmentContinuation = environmentContinuation;
            ResourceBrokerClient = resourceBrokerClient;
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "environment_force_suspend_action";

        private IEnvironmentContinuationOperations EnvironmentContinuation { get; }

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerClient { get; }

        /// <inheritdoc/>
        protected override async Task<CloudEnvironment> RunCoreAsync(
            Guid input,
            object transientState,
            IDiagnosticsLogger logger)
        {
            var record = await FetchAsync(input, logger.NewChildLogger());

            ValidateTargetLocation(record.Value.Location, logger);

            // If environment has OSDisk (a.k.a windows environment, force suspend via continuation operations).
            if (record.Value?.OSDisk != default)
            {
                await EnvironmentContinuation.ShutdownAsync(
                    input,
                    true,
                    LogBaseName,
                    logger.NewChildLogger());
                return record.Value;
            }

            // Deal with getting the state to the correct place
            var shutdownState = CloudEnvironmentState.Shutdown;
            if (record.Value?.Storage?.Type == ResourceType.StorageArchive)
            {
                shutdownState = CloudEnvironmentState.Archived;
            }

            // Set the state of the environment
            await EnvironmentStateManager.SetEnvironmentStateAsync(
                record.Value,
                shutdownState,
                CloudEnvironmentStateUpdateTriggers.ForceEnvironmentShutdown,
                null,
                null,
                logger);

            var computeIdToken = record.Value.Compute?.ResourceId;
            record.Value.Compute = null;

            // Update the database state.
            var updatedEnvironment = await Repository.UpdateAsync(record.Value, logger.NewChildLogger());
            record.ReplaceAndResetTransition(updatedEnvironment);

            // Delete the allocated resources.
            if (computeIdToken != null)
            {
                await ResourceBrokerClient.DeleteAsync(
                    input,
                    computeIdToken.Value,
                    logger.NewChildLogger());
            }

            return record.Value;
        }

        /// <inheritdoc/>
        protected override Task<bool> HandleExceptionAsync(
            Guid input,
            Exception ex,
            object transientState,
            IDiagnosticsLogger logger)
        {
            // Swallow all exceptions
            var markExceptionAsHandled = true;
            return Task.FromResult(markExceptionAsHandled);
        }
    }
}
