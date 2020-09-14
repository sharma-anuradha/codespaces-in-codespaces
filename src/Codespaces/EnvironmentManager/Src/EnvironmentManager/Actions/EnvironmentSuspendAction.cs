// <copyright file="EnvironmentSuspendAction.cs" company="Microsoft">
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
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Environment suspend action.
    /// </summary>
    public class EnvironmentSuspendAction : EnvironmentItemAction<EnvironmentSuspendActionInput, object>, IEnvironmentSuspendAction
    {
        private static readonly TimeSpan RegularSuspendTimeout = TimeSpan.FromMinutes(3);

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentSuspendAction"/> class.
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
        /// <param name="environmentMonitor">Target environment monitor.</param>
        /// <param name="environmentForceSuspendAction">Target environment force suspend action.</param>
        /// <param name="environmentManagerSettings">Target enviornment manager settings.</param>
        /// <param name="environmentArchivalTimeCalculator">Target enviornment archival time calculator.</param>
        public EnvironmentSuspendAction(
            IEnvironmentStateManager environmentStateManager,
            ICloudEnvironmentRepository repository,
            ICurrentLocationProvider currentLocationProvider,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo,
            IEnvironmentAccessManager environmentAccessManager,
            ISkuCatalog skuCatalog,
            ISkuUtils skuUtils,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerClient,
            IEnvironmentMonitor environmentMonitor,
            IEnvironmentForceSuspendAction environmentForceSuspendAction,
            EnvironmentManagerSettings environmentManagerSettings,
            IEnvironmentArchivalTimeCalculator environmentArchivalTimeCalculator)
            : base(environmentStateManager, repository, currentLocationProvider, currentUserProvider, controlPlaneInfo, environmentAccessManager, skuCatalog, skuUtils)
        {
            ResourceBrokerClient = Requires.NotNull(resourceBrokerClient, nameof(resourceBrokerClient));
            EnvironmentMonitor = Requires.NotNull(environmentMonitor, nameof(environmentMonitor));
            EnvironmentForceSuspendAction = Requires.NotNull(environmentForceSuspendAction, nameof(environmentForceSuspendAction));
            EnvironmentManagerSettings = Requires.NotNull(environmentManagerSettings, nameof(environmentManagerSettings));
            EnvironmentArchivalTimeCalculator = Requires.NotNull(environmentArchivalTimeCalculator, nameof(environmentArchivalTimeCalculator));
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "environment_suspend_action";

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerClient { get; }

        private IEnvironmentMonitor EnvironmentMonitor { get; }

        private IEnvironmentForceSuspendAction EnvironmentForceSuspendAction { get; }

        private EnvironmentManagerSettings EnvironmentManagerSettings { get; }

        private IEnvironmentArchivalTimeCalculator EnvironmentArchivalTimeCalculator { get; }

        /// <inheritdoc/>
        public async Task<CloudEnvironment> RunAsync(Guid environmentId, Guid computeResourceId, IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(environmentId, nameof(environmentId));

            var input = new EnvironmentSuspendActionInput(environmentId, false, computeResourceId);
            return await RunAsync(input, logger);
        }

        /// <inheritdoc/>
        public async Task<CloudEnvironment> RunAsync(Guid environmentId, bool isClientSuspend, IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(environmentId, nameof(environmentId));

            var input = new EnvironmentSuspendActionInput(environmentId, isClientSuspend, default);
            return await RunAsync(input, logger);
        }

        /// <inheritdoc/>
        protected override async Task<CloudEnvironment> RunCoreAsync(
            EnvironmentSuspendActionInput input,
            object transientObject,
            IDiagnosticsLogger logger)
        {
            var record = await FetchAsync(input, logger.NewChildLogger());

            ValidateEnvironment(record.Value);
            ValidateTargetLocation(record.Value.Location, logger);

            EnvironmentAccessManager.AuthorizeEnvironmentAccess(record.Value, nonOwnerScopes: null, logger);

            switch (record.Value.State)
            {
                case CloudEnvironmentState.Starting:
                case CloudEnvironmentState.ShuttingDown:
                    if (!IsTimedOut(record.Value))
                    {
                        if (input.IsClientSuspend)
                        {
                            return record.Value;
                        }
                        else
                        {
                            // Wait until timeout, then continue suspend
                            var timeUntilTimeout = RegularSuspendTimeout - (DateTime.UtcNow - record.Value.LastStateUpdated);
                            await Task.Delay(timeUntilTimeout >= TimeSpan.Zero ? timeUntilTimeout : TimeSpan.Zero);
                        }
                    }

                    break;

                case CloudEnvironmentState.Failed:
                case CloudEnvironmentState.Provisioning:
                case CloudEnvironmentState.Deleted:
                    if (input.IsClientSuspend)
                    {
                        throw new CodedValidationException((int)MessageCodes.ActionNotAllowedInThisState);
                    }
                    else
                    {
                        break;
                    }

                case CloudEnvironmentState.Available:
                case CloudEnvironmentState.Awaiting:
                case CloudEnvironmentState.Unavailable:
                    break;

                default:
                    throw new CodedValidationException((int)MessageCodes.ActionNotAllowedInThisState);
            }

            if (record.Value.State == CloudEnvironmentState.ShuttingDown ||
                record.Value.State == CloudEnvironmentState.Failed ||
                record.Value.State == CloudEnvironmentState.Provisioning ||
                record.Value.State == CloudEnvironmentState.Deleted ||
                (record.Value.Compute.ResourceId == default && input.AllocatedComputeResourceId == default))
            {
                // If the allocated compute is missing for the environment or the environment is otherwise unrecoverable,
                // force clean the environment details to put it in a recoverable state.
                return await EnvironmentForceSuspendAction.RunAsync(input, logger.NewChildLogger());
            }
            else
            {
                await EnvironmentStateManager.SetEnvironmentStateAsync(
                    record,
                    CloudEnvironmentState.ShuttingDown,
                    CloudEnvironmentStateUpdateTriggers.ShutdownEnvironment,
                    null,
                    null,
                    logger.NewChildLogger());

                record.PushTransition((environment) =>
                {
                    environment.Transitions.Resuming.ResetStatus(true);
                });

                if (await EnvironmentManagerSettings.DynamicEnvironmentArchivalTimeEnabled(logger.NewChildLogger()))
                {
                    var hoursToArchive = await EnvironmentArchivalTimeCalculator.ComputeHoursToArchival(record.Value, logger.NewChildLogger());
                    record.Value.ScheduledArchival = DateTime.UtcNow.AddHours(hoursToArchive);
                }

                // Update the database state.
                await Repository.UpdateTransitionAsync("cloudenvironment", record, logger);

                // Start the cleanup operation to shutdown environment.
                var resourceCleaningStatus = await ResourceBrokerClient.SuspendAsync(
                    input.Id,
                    input.AllocatedComputeResourceId == default ? record.Value.Compute.ResourceId : input.AllocatedComputeResourceId,
                    logger.NewChildLogger());
                logger.FluentAddValue("ResourceCleaningStatus", resourceCleaningStatus);

                // Kick off state transition monitoring.
                await EnvironmentMonitor.MonitorShutdownStateTransitionAsync(
                    record.Value.Id,
                    record.Value.Compute.ResourceId,
                    logger.NewChildLogger());

                return record.Value;
            }
        }

        // Returns true if environment has been in current state for longer than the suspend timeout.
        private bool IsTimedOut(CloudEnvironment environment)
        {
            return environment.LastStateUpdated.Add(RegularSuspendTimeout) < DateTime.UtcNow;
        }

        private void ValidateEnvironment(CloudEnvironment environment)
        {
            // Static Environment
            if (environment.Type == EnvironmentType.StaticEnvironment)
            {
                throw new CodedValidationException((int)MessageCodes.ShutdownStaticEnvironment);
            }
        }
    }
}
