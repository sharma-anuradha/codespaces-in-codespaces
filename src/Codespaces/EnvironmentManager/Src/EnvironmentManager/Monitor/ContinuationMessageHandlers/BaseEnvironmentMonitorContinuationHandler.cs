// <copyright file="BaseEnvironmentMonitorContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.RepairWorkflows;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers
{
    /// <summary>
    /// Base class for Environment Monitor Continuation Task Message Handlers.
    /// </summary>
    /// <typeparam name="TI">Target input.</typeparam>
    public abstract class BaseEnvironmentMonitorContinuationHandler<TI> : IContinuationTaskMessageHandler
        where TI : EnvironmentMonitorContinuationInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseEnvironmentMonitorContinuationHandler{TI}"/> class.
        /// </summary>
        /// <param name="environmentRepository">Target environment repository.</param>
        /// <param name="environmentRepairWorkflows">Target environment repair workflows.</param>
        /// <param name="latestHeartbeatMonitor">Target latest heartbeat monitor.</param>
        /// <param name="serviceProvider">Target environment service provider.</param>
        /// <param name="environmentMonitorSettings">Environment monitor settings.</param>
        public BaseEnvironmentMonitorContinuationHandler(
            ICloudEnvironmentRepository environmentRepository,
            IEnumerable<IEnvironmentRepairWorkflow> environmentRepairWorkflows,
            ILatestHeartbeatMonitor latestHeartbeatMonitor,
            IServiceProvider serviceProvider,
            EnvironmentMonitorSettings environmentMonitorSettings)
        {
            EnvironmentRepository = Requires.NotNull(environmentRepository, nameof(environmentRepository));
            EnvironmentRepairWorkflows = environmentRepairWorkflows.ToDictionary(x => x.WorkflowType);
            LatestHeartbeatMonitor = Requires.NotNull(latestHeartbeatMonitor, nameof(latestHeartbeatMonitor));
            ServiceProvider = Requires.NotNull(serviceProvider, nameof(serviceProvider));
            EnvironmentMonitorSettings = Requires.NotNull(environmentMonitorSettings, nameof(environmentMonitorSettings));
        }

        /// <summary>
        /// Gets the log base name.
        /// </summary>
        protected abstract string LogBaseName { get; }

        /// <summary>
        /// Gets the default Target that this handler targets.
        /// </summary>
        protected abstract string DefaultTarget { get; }

        /// <summary>
        /// Gets service Provider.
        /// </summary>
        protected IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Gets environment Repair Workflows.
        /// </summary>
        protected Dictionary<EnvironmentRepairActions, IEnvironmentRepairWorkflow> EnvironmentRepairWorkflows { get; }

        /// <summary>
        /// Gets latest Heartbeat Monitor.
        /// </summary>
        protected ILatestHeartbeatMonitor LatestHeartbeatMonitor { get; }

        /// <summary>
        /// Gets environment Monitor Settings.
        /// </summary>
        protected EnvironmentMonitorSettings EnvironmentMonitorSettings { get; }

        private ICloudEnvironmentRepository EnvironmentRepository { get; }

        /// <inheritdoc/>
        public bool CanHandle(ContinuationQueuePayload payload)
        {
            return payload.Target == DefaultTarget;
        }

        /// <inheritdoc/>
        public Task<ContinuationResult> Continue(ContinuationInput input, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                LogBaseName,
                (childLogger) => InnerContinue(input, childLogger));
        }

        /// <summary>
        /// Create result for unhealthy state to stop monitoring.
        /// </summary>
        /// <param name="state">target state.</param>
        /// <param name="reason">target reason.</param>
        /// <returns>result.</returns>
        protected static ContinuationResult CreateFinalResult(OperationState state, string reason)
        {
            return new ContinuationResult
            {
                Status = state,
                RetryAfter = TimeSpan.Zero,
                NextInput = default,
                ErrorReason = reason,
            };
        }

        /// <summary>
        /// Check if handler is enabled.
        /// </summary>
        /// <param name="logger">target logger.</param>
        /// <returns>result.</returns>
        protected abstract Task<bool> IsEnabledAsync(IDiagnosticsLogger logger);

        /// <summary>
        /// Create continuation result to get the monitoring started.
        /// </summary>
        /// <param name="operationInput">target input.</param>
        /// <param name="logger">target logger.</param>
        /// <returns>result.</returns>
        protected abstract ContinuationResult CreateContinuationResult(TI operationInput, IDiagnosticsLogger logger);

        /// <summary>
        /// Hnadle continuation message.
        /// </summary>
        /// <param name="operationInput">target input.</param>
        /// <param name="record">environment record.</param>
        /// <param name="logger">target logger.</param>
        /// <returns>result.</returns>
        protected abstract Task<ContinuationResult> RunOperationCoreAsync(TI operationInput, CloudEnvironment record, IDiagnosticsLogger logger);

        private async Task<ContinuationResult> InnerContinue(ContinuationInput input, IDiagnosticsLogger logger)
        {
            if (!(await IsEnabledAsync(logger)))
            {
                // Stop environment monitoring
                return CreateFinalResult(OperationState.Cancelled, "EnvironmentMonitoringDisabled");
            }

            var typedInput = input as TI;
            if (typedInput == null)
            {
                logger.FluentAddValue("HandlerInvalidInputType", true);

                throw new NotSupportedException($"Provided input type does not match target input type - {typeof(TI)}");
            }

            logger.FluentAddBaseValue("EnvironmentMonitorType", DefaultTarget)
                .FluentAddBaseValue("EnvironmentId", typedInput.EnvironmentId)
               .FluentAddBaseValue("ComputeResourceId", typedInput.ComputeResourceId)
               .FluentAddValue("HandlerOperationPreContinuationToken", typedInput.ContinuationToken);

            // Start Environment Hearbeat monitoring
            if (string.IsNullOrEmpty(typedInput.ContinuationToken))
            {
                typedInput.ContinuationToken = typedInput.EnvironmentId;

                // push message to monitor heartbeat for new environment.
                return CreateContinuationResult(typedInput, logger);
            }

            // Get env record
            var environment = await EnvironmentRepository.GetAsync(typedInput.EnvironmentId, logger.NewChildLogger());

            if (environment == null)
            {
                logger.FluentAddValue("HandlerFailedToFindResource", true);

                // return result with null next input
                return CreateFinalResult(OperationState.Cancelled, "EnvironmentRecordNotFound");
            }

            // Add environment details to telemetry.
            logger.AddCloudEnvironment(environment);

            // We don't need to do the resource check if we're a static environment.
            if (environment.Type != EnvironmentType.StaticEnvironment)
            {
                // Check Compute Id matches with message
                if (environment.Compute?.ResourceId != null && environment.Compute.ResourceId != typedInput.ComputeResourceId)
                {
                    // return result with null next input
                    return CreateFinalResult(OperationState.Cancelled, "EnvironmentResourceChanged");
                }
            }

            // Core continue
            var result = await RunOperationCoreAsync(typedInput, environment, logger);

            logger.FluentAddValue("HandlerBasePostContinuationToken", result.NextInput?.ContinuationToken)
                .FluentAddValue("HandlerBasePostStatus", result.Status)
                .FluentAddValue("HandlerBasePostErrorReason", result.ErrorReason)
                .FluentAddValue("HandlerBasePostRetryAfter", result.RetryAfter);

            return result;
        }
    }
}
