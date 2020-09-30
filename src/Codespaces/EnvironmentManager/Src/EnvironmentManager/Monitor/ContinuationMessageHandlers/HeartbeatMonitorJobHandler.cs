// <copyright file="HeartbeatMonitorJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Monitor;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers
{
    /// <summary>
    /// Delete Orphaned Resource Handler.
    /// </summary>
    public class HeartbeatMonitorJobHandler : EnvironmentMonitorJobHandlerBase<HeartbeatMonitorJobHandler.Payload>
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueId = "jobhandler-heartbeat-monitor";

        private static readonly List<CloudEnvironmentState> StateToProcess =
            new List<CloudEnvironmentState>
            {
                CloudEnvironmentState.Available,
                CloudEnvironmentState.Unavailable,
            };

        private static readonly List<CloudEnvironmentState> StateToStopTracking =
            new List<CloudEnvironmentState>
            {
                CloudEnvironmentState.Shutdown,
                CloudEnvironmentState.Archived,
                CloudEnvironmentState.Failed,
                CloudEnvironmentState.Deleted,
            };

        /// <summary>
        /// Initializes a new instance of the <see cref="HeartbeatMonitorContinuationHandler"/> class.
        /// </summary>
        /// <param name="environmentRepository">Target environment repository.</param>
        /// <param name="heartbeatRepository">Target environment heartbeat repository.</param>
        /// <param name="latestHeartbeatMonitor">Target latest Heartbeat Monitor.</param>
        /// <param name="environmentMonitor">Environment monitor.</param>
        /// <param name="environmentMonitorSettings">Environment monitor settings.</param>
        public HeartbeatMonitorJobHandler(
            ICloudEnvironmentRepository environmentRepository,
            ICloudEnvironmentHeartbeatRepository heartbeatRepository,
            ILatestHeartbeatMonitor latestHeartbeatMonitor,
            IEnvironmentMonitor environmentMonitor,
            IEnvironmentSuspendAction environmentSuspendAction,
            IEnvironmentForceSuspendAction environmentForceSuspendAction,
            IEnvironmentUnavailableAction environmentUnavailableAction,
            EnvironmentMonitorSettings environmentMonitorSettings)

            : base(environmentRepository, latestHeartbeatMonitor, environmentMonitor, environmentMonitorSettings)
        {
            HeartbeatRepository = heartbeatRepository;
            EnvironmentSuspendAction = Requires.NotNull(environmentSuspendAction, nameof(environmentSuspendAction));
            EnvironmentForceSuspendAction = Requires.NotNull(environmentForceSuspendAction, nameof(EnvironmentForceSuspendAction));
            EnvironmentUnavailableAction = Requires.NotNull(environmentUnavailableAction, nameof(EnvironmentUnavailableAction));
        }

        private ICloudEnvironmentHeartbeatRepository HeartbeatRepository { get; }

        private IEnvironmentSuspendAction EnvironmentSuspendAction { get; }

        private IEnvironmentForceSuspendAction EnvironmentForceSuspendAction { get; }

        private IEnvironmentUnavailableAction EnvironmentUnavailableAction { get; }

        /// <inheritdoc/>
        public override string QueueId => DefaultQueueId;

        /// <inheritdoc/>
        protected override string LogBaseName => "HeartbeatMonitor";

        /// <inheritdoc/>
        protected override async Task<bool> IsEnabledAsync(IDiagnosticsLogger logger)
        {
            return await EnvironmentMonitorSettings.EnableHeartbeatMonitoring(logger.NewChildLogger());
        }

        /// <inheritdoc/>
        protected override async Task<(OperationState operationState, string reason)> HandleJobAsync(Payload payload, CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            // Check the Environment state is valid.
            if (StateToStopTracking.Contains(environment.State) || (environment.Compute?.ResourceId == null && environment.Type != EnvironmentType.StaticEnvironment))
            {
                // return result with null next input
                return (OperationState.Cancelled, EnvironmentMonitorConstants.StopHeartbeatMonitoringStateReason);
            }

#pragma warning disable CS0618 // Type or member is obsolete
            DateTime? lastUpdatedByHeartBeat = environment.LastUpdatedByHeartBeat;
#pragma warning restore CS0618 // Type or member is obsolete
            if (!string.IsNullOrEmpty(environment.HeartbeatResourceId))
            {
                var heartbeatRecord = await HeartbeatRepository.GetAsync(environment.HeartbeatResourceId, logger.NewChildLogger());
                lastUpdatedByHeartBeat = heartbeatRecord.LastUpdatedByHeartBeat;
            }

            bool hasHeartbeatTimeExpired =
                (LatestHeartbeatMonitor.LastHeartbeatReceived != default && lastUpdatedByHeartBeat == default)
                || (LatestHeartbeatMonitor.LastHeartbeatReceived != default
                && LatestHeartbeatMonitor.LastHeartbeatReceived > lastUpdatedByHeartBeat + TimeSpan.FromSeconds(EnvironmentMonitorConstants.HeartbeatIntervalInSeconds)
                && lastUpdatedByHeartBeat < DateTime.UtcNow.AddMinutes(-EnvironmentMonitorConstants.HeartbeatTimeoutInMinutes));

            logger.FluentAddBaseValue(nameof(lastUpdatedByHeartBeat), lastUpdatedByHeartBeat);

            if (environment.Type != EnvironmentType.StaticEnvironment)
            {
                if (environment.State == CloudEnvironmentState.Unavailable)
                {
                    var unavailableTimeout = await EnvironmentMonitorSettings.UnavailableEnvironmentTimeout(logger);

                    if (environment.LastStateUpdated + unavailableTimeout < DateTime.UtcNow)
                    {
                        // environment has been stuck in unavailable (for more than an hour by default)
                        if (await EnvironmentMonitorSettings.EnableUnavailableEnvironmentHeartbeatMonitoring(logger))
                        {
                            await EnvironmentSuspendAction.RunAsync(Guid.Parse(environment.Id), false, logger.NewChildLogger());
                            return (OperationState.Failed, EnvironmentMonitorConstants.UnhealthyUnavailableHeartbeatReason);
                        }
                    }
                }

                if (StateToProcess.Contains(environment.State) && hasHeartbeatTimeExpired)
                {
                    await EnvironmentSuspendAction.RunAsync(Guid.Parse(environment.Id), false, logger.NewChildLogger());
                    return (OperationState.Failed, EnvironmentMonitorConstants.NoHeartbeatReason);
                }
            }
            else
            {
                if (environment.State == CloudEnvironmentState.Available && hasHeartbeatTimeExpired)
                {
                    await EnvironmentUnavailableAction.RunAsync(Guid.Parse(environment.Id), EnvironmentMonitorConstants.EnvironmentUnavailableReason, logger.NewChildLogger());

                    await EnvironmentMonitor.MonitorHeartbeatAsync(environment.Id, environment.Compute?.ResourceId, logger.NewChildLogger());
                    return (OperationState.Failed, EnvironmentMonitorConstants.NoHeartbeatMarkingUnavailableReason);
                }
                else if (environment.State == CloudEnvironmentState.Unavailable && hasHeartbeatTimeExpired)
                {
                    await EnvironmentMonitor.MonitorHeartbeatAsync(environment.Id, environment.Compute?.ResourceId, logger.NewChildLogger());
                    return (OperationState.Failed, EnvironmentMonitorConstants.NoHeartbeatForUnavailableEnvironmentReason);
                }
            }

            // Starts the next monitoring cycle
            await EnvironmentMonitor.MonitorHeartbeatAsync(environment.Id, environment.Compute?.ResourceId, logger.NewChildLogger());

            // compute has healthy heartbeat, return next input
            return (OperationState.Succeeded, EnvironmentMonitorConstants.HealthyHeartbeatReason);
        }

        [JobPayload(nameOption: JobPayloadNameOption.Name)]
        public class Payload : EnvironmentMonitorJobPayloadBase
        {
        }
    }
}
