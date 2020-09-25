// <copyright file="EnvironmentFailAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts.Actions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    public class EnvironmentFailAction : EnvironmentItemAction<EnvironmentFailActionInput, object>, IEnvironmentFailAction
    {
        public EnvironmentFailAction(
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

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerClient { get; }

        /// <inheritdoc/>
        protected override string LogBaseName => "environment_fail_action";

        public Task<CloudEnvironment> RunAsync(Guid environmentId, string reason, IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(environmentId, nameof(environmentId));

            var input = new EnvironmentFailActionInput(environmentId, reason);

            return RunAsync(input, logger);
        }

        protected override async Task<CloudEnvironment> RunCoreAsync(EnvironmentFailActionInput input, object transientState, IDiagnosticsLogger logger)
        {
            var record = await FetchAsync(input, logger.NewChildLogger());
            var cloudEnvironment = record.Value;

            // Update state to be failed
            await EnvironmentStateManager.SetEnvironmentStateAsync(
                record,
                CloudEnvironmentState.Failed,
                nameof(EnvironmentFailAction),
                input.Reason,
                null,
                logger.NewChildLogger());

            var cloudEnvironmentId = Guid.Parse(cloudEnvironment.Id);

            // Delete the allocated resources.
            if (cloudEnvironment.Compute != null)
            {
                await ResourceBrokerClient.DeleteAsync(cloudEnvironmentId, cloudEnvironment.Compute.ResourceId, logger.NewChildLogger());
            }

            if (cloudEnvironment.OSDisk != null)
            {
                await ResourceBrokerClient.DeleteAsync(cloudEnvironmentId, cloudEnvironment.OSDisk.ResourceId, logger.NewChildLogger());
            }

            if (cloudEnvironment.Storage != null)
            {
                await ResourceBrokerClient.DeleteAsync(cloudEnvironmentId, cloudEnvironment.Storage.ResourceId, logger.NewChildLogger());
            }

            // Update the database state
            await Repository.UpdateTransitionAsync("cloudenvironment", record, logger);

            return record.Value;
        }

        /// <inheritdoc/>
        protected override Task<bool> HandleExceptionAsync(
            EnvironmentFailActionInput input,
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
