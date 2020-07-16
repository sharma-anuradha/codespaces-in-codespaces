// <copyright file="EnvironmentUpdateStatusAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Environment Update Status Action.
    /// </summary>
    public class EnvironmentUpdateStatusAction : EnvironmentItemAction<EnvironmentUpdateStatusActionInput>, IEnvironmentUpdateStatusAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentUpdateStatusAction"/> class.
        /// </summary>
        /// <param name="environmentStateManager">Target environment state manager.</param>
        /// <param name="repository">Target repository.</param>
        /// <param name="currentLocationProvider">Target current location provider.</param>
        /// <param name="currentUserProvider">Target current user provider.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        /// <param name="environmentAccessManager">Target environment access manager.</param>
        public EnvironmentUpdateStatusAction(
            IEnvironmentStateManager environmentStateManager,
            ICloudEnvironmentRepository repository,
            ICurrentLocationProvider currentLocationProvider,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo,
            IEnvironmentAccessManager environmentAccessManager)
            : base(environmentStateManager, repository, currentLocationProvider, currentUserProvider, controlPlaneInfo, environmentAccessManager)
        {
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "environment_update_status_action";

        /// <inheritdoc/>
        public Task<CloudEnvironment> Run(Guid cloudenvironmentId, CloudEnvironmentState newState, string trigger, string reason, IDiagnosticsLogger logger)
        {
            var input = new EnvironmentUpdateStatusActionInput(cloudenvironmentId)
            {
                NewState = newState,
                Trigger = trigger,
                Reason = reason,
            };

            return Run(input, logger);
        }

        /// <inheritdoc/>
        protected override EnvironmentTransition BuildTransition(CloudEnvironment model)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override async Task<CloudEnvironment> RunCoreAsync(EnvironmentUpdateStatusActionInput input, IDiagnosticsLogger logger)
        {
            // Fetch Record
            var record = await FetchAsync(input, logger.NewChildLogger());
            if (record == null)
            {
                throw new EntityNotFoundException($"Target '{input.Id}' not found.");
            }

            var cloudEnvironment = record.Value;

            if (input.NewState != default && input.NewState != record.Value.State)
            {
                // TODO: Need to switch things over to EntityTransition doing the work!!!!
                await EnvironmentStateManager.SetEnvironmentStateAsync(
                    cloudEnvironment, input.NewState, input.Trigger, input.Reason, null, logger.NewChildLogger());

                // TODO: Need to switch things over to EntityTransition doing the work!!!!
                cloudEnvironment = await Repository.UpdateAsync(cloudEnvironment, logger.NewChildLogger());
            }

            return cloudEnvironment;
        }
    }
}
