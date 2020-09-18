// <copyright file="ShutdownEnvironmentContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    /// <summary>
    /// Shutdown environment continuation handler.
    /// </summary>
    public class ShutdownEnvironmentContinuationHandler :
         BaseContinuationTaskMessageHandler<ShutdownEnvironmentContinuationInput>, IShutdownEnvironmentContinuationHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShutdownEnvironmentContinuationHandler"/> class.
        /// </summary>
        /// <param name="cloudEnvironmentRepository">Cloud environment repository.</param>
        /// <param name="resourceBrokerHttpClient">Resource broker httpclient.</param>
        /// <param name="environmentStateManager">Environment state manager.</param>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="environmentManagerSettings">Environment Manager settings.</param>
        /// <param name="archivalTimeCalculator">Archival Time Calculator.</param>
        public ShutdownEnvironmentContinuationHandler(
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            IEnvironmentStateManager environmentStateManager,
            IServiceProvider serviceProvider,
            EnvironmentManagerSettings environmentManagerSettings,
            IEnvironmentArchivalTimeCalculator archivalTimeCalculator)
            : base(cloudEnvironmentRepository)
        {
            ResourceBrokerHttpClient = Requires.NotNull(resourceBrokerHttpClient, nameof(resourceBrokerHttpClient));
            EnvironmentStateManager = Requires.NotNull(environmentStateManager, nameof(environmentStateManager));
            ServiceProvider = Requires.NotNull(serviceProvider, nameof(serviceProvider));
            EnvironmentManagerSettings = Requires.NotNull(environmentManagerSettings, nameof(environmentManagerSettings));
            ArchivalTimeCalculator = Requires.NotNull(archivalTimeCalculator, nameof(archivalTimeCalculator));
        }

        /// <summary>
        /// Gets target name.
        /// </summary>
        public static string DefaultQueueTarget => "JobShutdownEnvironment";

        /// <inheritdoc/>
        protected override string LogBaseName => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override EnvironmentOperation Operation => EnvironmentOperation.ShuttingDown;

        /// <inheritdoc/>
        protected override string DefaultTarget => DefaultQueueTarget;

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerHttpClient { get; }

        private IEnvironmentStateManager EnvironmentStateManager { get; }

        private IServiceProvider ServiceProvider { get; }

        private EnvironmentManagerSettings EnvironmentManagerSettings { get; }

        private IEnvironmentArchivalTimeCalculator ArchivalTimeCalculator { get; }

        /// <inheritdoc/>
        protected override TransitionState FetchOperationTransition(ShutdownEnvironmentContinuationInput input, EnvironmentRecordRef record, IDiagnosticsLogger logger)
        {
            return record.Value.Transitions.ShuttingDown;
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
            ShutdownEnvironmentContinuationInput operationInput,
            EnvironmentRecordRef record,
            string trigger,
            IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;
            logger.LogError($"{LogBaseName}_failed");

            return await base.FailOperationCleanupCoreAsync(operationInput, record, trigger, logger);
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationResult> RunOperationCoreAsync(ShutdownEnvironmentContinuationInput operationInput, EnvironmentRecordRef record, IDiagnosticsLogger logger)
        {
            if (record.Value == default || record.Value.State == CloudEnvironmentState.Shutdown)
            {
                // No environment record or if it is marked shutdown. Return success.
                return new ContinuationResult { Status = OperationState.Succeeded, };
            }

            // skip to delete step when forced to suspend.
            if (operationInput.Force && operationInput.CurrentState == ShutdownEnvironmentContinuationInputState.CheckComputeCleanupStatus)
            {
                operationInput.CurrentState = ShutdownEnvironmentContinuationInputState.ComputeDelete;
            }

            // Fast track marking shutdown when resource is not found.
            if (operationInput.CurrentState != ShutdownEnvironmentContinuationInputState.MarkShutdown &&
                record.Value.Compute?.ResourceId == default)
            {
                // No compute resource, it has been deleted some how.
                operationInput.CurrentState = ShutdownEnvironmentContinuationInputState.MarkShutdown;
            }

            switch (operationInput.CurrentState)
            {
                case ShutdownEnvironmentContinuationInputState.CheckComputeCleanupStatus:
                    return await CheckComputeCleanupStatusAsync(operationInput, record, logger);

                case ShutdownEnvironmentContinuationInputState.ComputeDelete:
                    return await DeleteComputeAsync(operationInput, record, logger);

                case ShutdownEnvironmentContinuationInputState.CheckComputeDeleteStatus:
                    return await CheckComputeDeleteStatusAsync(operationInput, record, logger);

                case ShutdownEnvironmentContinuationInputState.MarkShutdown:
                    return await MarkShutdownAsync(operationInput, record, logger);

                default:
                    return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidEnvironmentShutdownState" };
            }
        }

        private async Task<ContinuationResult> CheckComputeDeleteStatusAsync(ShutdownEnvironmentContinuationInput operationInput, EnvironmentRecordRef record, IDiagnosticsLogger logger)
        {
            var environmentId = Guid.Parse(record.Value.Id);
            var computeId = record.Value.Compute.ResourceId;

            try
            {
                await ResourceBrokerHttpClient.StatusAsync(environmentId, computeId, logger.NewChildLogger());
            }
            catch (Exception ex) when (ex is RemoteInvocationException || (ex is HttpResponseStatusException httpResponseStatusException && httpResponseStatusException.StatusCode == System.Net.HttpStatusCode.NotFound))
            {
                operationInput.CurrentState = ShutdownEnvironmentContinuationInputState.MarkShutdown;
                return new ContinuationResult { NextInput = operationInput, Status = OperationState.InProgress, };
            }

            return new ContinuationResult { NextInput = operationInput, Status = OperationState.InProgress, RetryAfter = TimeSpan.FromSeconds(2), };
        }

        private async Task<ContinuationResult> DeleteComputeAsync(ShutdownEnvironmentContinuationInput operationInput, EnvironmentRecordRef record, IDiagnosticsLogger logger)
        {
            var environmentId = Guid.Parse(record.Value.Id);
            var computeId = record.Value.Compute.ResourceId;

            try
            {
                await ResourceBrokerHttpClient.DeleteAsync(environmentId, computeId, logger.NewChildLogger());
            }
            catch (Exception ex) when (ex is RemoteInvocationException || (ex is HttpResponseStatusException httpResponseStatusException && httpResponseStatusException.StatusCode == System.Net.HttpStatusCode.NotFound))
            {
                operationInput.CurrentState = ShutdownEnvironmentContinuationInputState.MarkShutdown;
                return new ContinuationResult { NextInput = operationInput, Status = OperationState.InProgress, };
            }

            operationInput.CurrentState = ShutdownEnvironmentContinuationInputState.CheckComputeDeleteStatus;
            return new ContinuationResult { NextInput = operationInput, Status = OperationState.InProgress, };
        }

        private async Task<ContinuationResult> MarkShutdownAsync(ShutdownEnvironmentContinuationInput operationInput, EnvironmentRecordRef record, IDiagnosticsLogger logger)
        {
            await EnvironmentStateManager.SetEnvironmentStateAsync(record.Value, CloudEnvironmentState.Shutdown, CloudEnvironmentStateUpdateTriggers.ShutdownEnvironment, null, null, logger.NewChildLogger());

            var didUpdate = await UpdateRecordAsync(
                    operationInput,
                    record,
                    async (environment, innerLogger) =>
                    {
                        record.Value.State = CloudEnvironmentState.Shutdown;
                        record.Value.Compute = null;

                        if (await EnvironmentManagerSettings.DynamicEnvironmentArchivalTimeEnabled(logger.NewChildLogger()))
                        {
                            double hoursToArchive = await ArchivalTimeCalculator.ComputeHoursToArchival(record.Value, logger.NewChildLogger());
                            record.Value.ScheduledArchival = DateTime.UtcNow.AddHours(hoursToArchive);
                        }

                        return true;
                    },
                    logger);

            if (!didUpdate)
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "FailedToUpdateEnvironmentRecord" };
            }

            return new ContinuationResult { Status = OperationState.Succeeded, };
        }

        private async Task<ContinuationResult> CheckComputeCleanupStatusAsync(ShutdownEnvironmentContinuationInput operationInput, EnvironmentRecordRef record, IDiagnosticsLogger logger)
        {
            var environmentId = Guid.Parse(record.Value.Id);
            var computeId = record.Value.Compute.ResourceId;

            var computeStatus = default(StatusResponseBody);
            try
            {
                computeStatus = await ResourceBrokerHttpClient.StatusAsync(environmentId, computeId, logger.NewChildLogger());
            }
            catch (Exception ex) when (ex is RemoteInvocationException || (ex is HttpResponseStatusException httpResponseStatusException && httpResponseStatusException.StatusCode == System.Net.HttpStatusCode.NotFound))
            {
                operationInput.CurrentState = ShutdownEnvironmentContinuationInputState.MarkShutdown;
                return new ContinuationResult { NextInput = operationInput, Status = OperationState.InProgress, };
            }

            if (computeStatus?.CleanupStatus == OperationState.Succeeded)
            {
                operationInput.CurrentState = ShutdownEnvironmentContinuationInputState.ComputeDelete;
                return new ContinuationResult { NextInput = operationInput, Status = OperationState.InProgress, };
            }
            else
            {
                return new ContinuationResult { NextInput = operationInput, Status = OperationState.InProgress, RetryAfter = TimeSpan.FromSeconds(2), };
            }
        }
    }
}
