// <copyright file="EnvironmentUnavailableAction.cs" company="Microsoft">
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
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    public class EnvironmentUnavailableAction : EnvironmentItemAction<EnvironmentUnavailableActionInput, object>, IEnvironmentUnavailableAction
    {
        public EnvironmentUnavailableAction(
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
        protected override string LogBaseName => "environment_unavailable_action";

        public Task<CloudEnvironment> RunAsync(Guid environmentId, string reason, IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(environmentId, nameof(environmentId));

            var input = new EnvironmentUnavailableActionInput(environmentId, reason);

            return RunAsync(input, logger);
        }

        protected override async Task<CloudEnvironment> RunCoreAsync(EnvironmentUnavailableActionInput input, object transientState, IDiagnosticsLogger logger)
        {
            var record = await FetchAsync(input, logger.NewChildLogger());

            await EnvironmentStateManager.SetEnvironmentStateAsync(
                record,
                CloudEnvironmentState.Unavailable,
                nameof(EnvironmentUnavailableAction),
                input.Reason,
                null,
                logger);

            await Repository.UpdateTransitionAsync("cloudenvironment", record, logger);

            return record.Value;
        }

        /// <inheritdoc/>
        protected override Task<bool> HandleExceptionAsync(
            EnvironmentUnavailableActionInput input,
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
