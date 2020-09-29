// <copyright file="EnvironmentForceSuspendAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Environment force suspend action.
    /// </summary>
    public class EnvironmentForceSuspendAction : EnvironmentItemAction<EnvironmentSuspendActionInput, object>, IEnvironmentForceSuspendAction
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
            IServiceProvider serviceProvider,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerClient)
            : base(environmentStateManager, repository, currentLocationProvider, currentUserProvider, controlPlaneInfo, environmentAccessManager, skuCatalog, skuUtils)
        {
            ServiceProvider = serviceProvider;
            ResourceBrokerClient = resourceBrokerClient;
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "environment_force_suspend_action";

        private IServiceProvider ServiceProvider { get; }

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerClient { get; }

        /// <inheritdoc/>
        public Task<CloudEnvironment> RunAsync(Guid environmentId, IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(environmentId, nameof(environmentId));

            var input = new EnvironmentSuspendActionInput(environmentId);

            return RunAsync(input, logger);
        }

        /// <inheritdoc/>
        protected override async Task<CloudEnvironment> RunCoreAsync(
            EnvironmentSuspendActionInput input,
            object transientState,
            IDiagnosticsLogger logger)
        {
            var record = await FetchAsync(input, logger.NewChildLogger());

            ValidateTargetLocation(record.Value.Location, logger);

            // If environment has OSDisk (a.k.a windows environment, force suspend via continuation operations).
            if (record.Value?.OSDisk != default)
            {
                var environmentContinuation = ServiceProvider.GetRequiredService<IEnvironmentContinuationOperations>();
                
                await environmentContinuation.ShutdownAsync(
                    input.Id,
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
                record,
                shutdownState,
                CloudEnvironmentStateUpdateTriggers.ForceEnvironmentShutdown,
                null,
                null,
                logger);

            var computeIdToken = record.Value.Compute?.ResourceId;

            record.PushTransition((environment) =>
            {
                environment.Compute = null;
            });

            // Update the database state.
            await Repository.UpdateTransitionAsync("cloudenvironment", record, logger);

            // Delete the allocated resources.
            if (computeIdToken != null)
            {
                await ResourceBrokerClient.DeleteAsync(
                    input.Id,
                    computeIdToken.Value,
                    logger.NewChildLogger());
            }

            // Delete the allocated compute resource that is not yet persisted, if any.
            // This can happen during resume, if DB write fails after resource allocation
            if (input.AllocatedComputeResourceId != default && input.AllocatedComputeResourceId != computeIdToken)
            {
                await ResourceBrokerClient.DeleteAsync(
                    input.Id,
                    input.AllocatedComputeResourceId,
                    logger.NewChildLogger());
            }

            return record.Value;
        }

        /// <inheritdoc/>
        protected override Task<bool> HandleExceptionAsync(
            EnvironmentSuspendActionInput input,
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
