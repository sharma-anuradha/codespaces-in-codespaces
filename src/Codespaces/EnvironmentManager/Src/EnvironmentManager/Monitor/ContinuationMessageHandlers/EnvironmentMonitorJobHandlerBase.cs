// <copyright file="EnvironmentMonitorJobHandlerBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers
{
    /// <summary>
    /// Base class for Environment Monitor Continuation Task Message Handlers.
    /// </summary>
    /// <typeparam name="TPayload">Target payload.</typeparam>
    public abstract class EnvironmentMonitorJobHandlerBase<TPayload> : JobHandlerPayloadBase<TPayload>, IJobHandlerTarget
        where TPayload : EnvironmentMonitorJobPayloadBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseEnvironmentMonitorContinuationHandler{TI}"/> class.
        /// </summary>
        /// <param name="environmentRepository">Target environment repository.</param>
        /// <param name="latestHeartbeatMonitor">Target latest heartbeat monitor.</param>
        /// <param name="environmentMonitor">Environment monitor.</param>
        /// <param name="environmentMonitorSettings">Environment monitor settings.</param>
        public EnvironmentMonitorJobHandlerBase(
            ICloudEnvironmentRepository environmentRepository,
            ILatestHeartbeatMonitor latestHeartbeatMonitor,
            IEnvironmentMonitor environmentMonitor,
            EnvironmentMonitorSettings environmentMonitorSettings)
        {
            EnvironmentRepository = Requires.NotNull(environmentRepository, nameof(environmentRepository));
            LatestHeartbeatMonitor = Requires.NotNull(latestHeartbeatMonitor, nameof(latestHeartbeatMonitor));
            EnvironmentMonitor = Requires.NotNull(environmentMonitor, nameof(environmentMonitor));
            EnvironmentMonitorSettings = Requires.NotNull(environmentMonitorSettings, nameof(environmentMonitorSettings));
        }

        /// <inheritdoc/>
        public IJobHandler JobHandler => this;

        /// <inheritdoc/>
        public abstract string QueueId { get; }

        /// <inheritdoc/>
        public AzureLocation? Location => null;

        /// <summary>
        /// Gets the log base name.
        /// </summary>
        protected abstract string LogBaseName { get; }

        /// <summary>
        /// Gets latest Heartbeat Monitor.
        /// </summary>
        protected ILatestHeartbeatMonitor LatestHeartbeatMonitor { get; }

        /// <summary>
        /// Gets environment Monitor Settings.
        /// </summary>
        protected EnvironmentMonitorSettings EnvironmentMonitorSettings { get; }

        /// <summary>
        /// Gets environment Monitor.
        /// </summary>
        protected IEnvironmentMonitor EnvironmentMonitor { get; }

        private ICloudEnvironmentRepository EnvironmentRepository { get; }

        /// <summary>
        /// Check if handler is enabled.
        /// </summary>
        /// <param name="logger">target logger.</param>
        /// <returns>result.</returns>
        protected abstract Task<bool> IsEnabledAsync(IDiagnosticsLogger logger);

        private static void LogFinalResult(IDiagnosticsLogger logger, OperationState state, string reason)
        {
            logger.FluentAddBaseValue("OperationState", state)
               .FluentAddBaseValue("ErrorReason", reason)
               .LogInfo("environment_monitor_job_handler_final_result");
        }

        /// <inheritdoc/>
        protected override async Task HandleJobAsync(TPayload payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            if (!(await IsEnabledAsync(logger)))
            {
                // Stop environment monitoring
                LogFinalResult(logger, OperationState.Cancelled, "EnvironmentMonitoringDisabled");
                return;
            }

            logger.FluentAddBaseValue("EnvironmentId", payload.EnvironmentId)
               .FluentAddBaseValue("ComputeResourceId", payload.ComputeResourceId);

            // Get env record
            var environment = await EnvironmentRepository.GetAsync(payload.EnvironmentId, logger.NewChildLogger());

            if (environment == null)
            {
                logger.FluentAddValue("HandlerFailedToFindResource", true);

                // return result with null next input
                LogFinalResult(logger, OperationState.Cancelled, "EnvironmentRecordNotFound");
                return;
            }

            // Add environment details to telemetry.
            logger.AddCloudEnvironment(environment);

            // We don't need to do the resource check if we're a static environment.
            if (environment.Type != EnvironmentType.StaticEnvironment)
            {
                // Check Compute Id matches with message
                if (environment.Compute?.ResourceId != null && environment.Compute.ResourceId != payload.ComputeResourceId)
                {
                    // return result with null next input
                    LogFinalResult(logger, OperationState.Cancelled, "EnvironmentResourceChanged");
                    return;
                }
            }

            // Core continue
            var result = await HandleJobAsync(payload, environment, logger);
            LogFinalResult(logger, result.operationState, result.reason);
        }

        protected abstract Task<(OperationState operationState, string reason)> HandleJobAsync(TPayload payload, CloudEnvironment environment, IDiagnosticsLogger logger);
    }
}
