// <copyright file="EnvironmentMonitor.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Monitor;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Environment monitor operations.
    /// </summary>
    public class EnvironmentMonitor : IEnvironmentMonitor
    {
        private const string LogBaseName = "environment_monitor";

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentMonitor"/> class.
        /// </summary>
        /// <param name="activator">Target activator.</param>
        /// <param name="environmentMonitorSettings">Target settings.</param>
        /// <param name="jobQueueProducerFactory">Job Queue producer factory instance.</param>
        /// <param name="configurationReader">Configuration reader.</param>
        public EnvironmentMonitor(
             IContinuationTaskActivator activator,
             EnvironmentMonitorSettings environmentMonitorSettings,
             IJobQueueProducerFactory jobQueueProducerFactory,
             IConfigurationReader configurationReader)
        {
            Activator = activator;
            EnvironmentMonitorSettings = environmentMonitorSettings;
            JobQueueProducerFactory = jobQueueProducerFactory;
            ConfigurationReader = configurationReader;
        }

        private IContinuationTaskActivator Activator { get; }

        private EnvironmentMonitorSettings EnvironmentMonitorSettings { get; }

        private IJobQueueProducerFactory JobQueueProducerFactory { get; }

        private IConfigurationReader ConfigurationReader { get; }

        /// <inheritdoc/>
        public async Task MonitorHeartbeatAsync(string environmentId, Guid? computeId, IDiagnosticsLogger logger)
        {
            // Check for flighting switch
            if (!await EnvironmentMonitorSettings.EnableHeartbeatMonitoring(logger.NewChildLogger()))
            {
                // Stop environment monitoring
                return;
            }

            var loggingProperties = new Dictionary<string, string>();

            if (await IsJobContinuationHandlerEnabledAsync(logger))
            {
                await JobQueueProducerFactory.GetOrCreate(HeartbeatMonitorJobHandler.DefaultQueueId).AddJobAsync(
                    new HeartbeatMonitorJobHandler.Payload()
                    {
                        EnvironmentId = environmentId,
                        ComputeResourceId = computeId ?? default(Guid),
                    },
                    new JobPayloadOptions() { InitialVisibilityDelay = TimeSpan.FromMinutes(EnvironmentMonitorConstants.HeartbeatTimeoutInMinutes + EnvironmentMonitorConstants.BufferInMinutes) },
                    logger,
                    CancellationToken.None);
            }
            else
            {
                var input = new HeartbeatMonitorInput()
                {
                    EnvironmentId = environmentId,
                    ComputeResourceId = computeId ?? default(Guid),
                    ContinuationToken = string.Empty,
                };

                var target = HeartbeatMonitorContinuationHandler.DefaultQueueTarget;

                var result = await Activator.Execute(target, input, logger, null, loggingProperties);
                if (result.Status != OperationState.InProgress)
                {
                    throw new EnvironmentMonitorInitializationException(environmentId);
                }
            }
        }

        /// <inheritdoc/>
        public async Task MonitorProvisioningStateTransitionAsync(string environmentId, Guid computeId, IDiagnosticsLogger logger)
        {
            // Check for flighting switch (second-level flag for provisioning state)
            if (!await EnvironmentMonitorSettings.EnableProvisioningStateTransitionMonitoring(logger.NewChildLogger()))
            {
                // Skip this monitor
                return;
            }

            var input = new EnvironmentStateTransitionInput()
            {
                EnvironmentId = environmentId,
                ComputeResourceId = computeId,
                CurrentState = CloudEnvironmentState.Provisioning,
                TargetState = CloudEnvironmentState.Available,
                TransitionTimeout = await EnvironmentMonitorSettings.ProvisionEnvironmentAcknowledgementTimeoutInSeconds(logger.NewChildLogger()),
            };

            await MonitorStateTransitionAsync(input, logger);
        }

        /// <inheritdoc/>
        public async Task MonitorProvisioningStateTransitionAsync(string environmentId, Guid computeId, TimeSpan timeout, IDiagnosticsLogger logger)
        {
            // Check for flighting switch (second-level flag for provisioning state)
            if (!await EnvironmentMonitorSettings.EnableProvisioningStateTransitionMonitoring(logger.NewChildLogger()))
            {
                // Skip this monitor
                return;
            }

            var input = new EnvironmentStateTransitionInput()
            {
                EnvironmentId = environmentId,
                ComputeResourceId = computeId,
                CurrentState = CloudEnvironmentState.Provisioning,
                TargetState = CloudEnvironmentState.Available,
                TransitionTimeout = timeout,
            };

            await MonitorStateTransitionAsync(input, logger);
        }

        /// <inheritdoc/>
        public async Task MonitorResumeStateTransitionAsync(string environmentId, Guid computeId, IDiagnosticsLogger logger)
        {
            var input = new EnvironmentStateTransitionInput()
            {
                EnvironmentId = environmentId,
                ComputeResourceId = computeId,
                CurrentState = CloudEnvironmentState.Starting,
                TargetState = CloudEnvironmentState.Available,
                TransitionTimeout = await EnvironmentMonitorSettings.ResumeEnvironmentTimeout(logger.NewChildLogger()),
            };

            await MonitorStateTransitionAsync(input, logger);
        }

        /// <inheritdoc/>
        public async Task MonitorExportStateTransitionAsync(string environmentId, Guid computeId, IDiagnosticsLogger logger)
        {
            var input = new EnvironmentStateTransitionInput()
            {
                EnvironmentId = environmentId,
                ComputeResourceId = computeId,
                CurrentState = CloudEnvironmentState.Exporting,
                TargetState = CloudEnvironmentState.Shutdown,
                TransitionTimeout = await EnvironmentMonitorSettings.ExportEnvironmentTimeout(logger.NewChildLogger()),
            };

            await MonitorStateTransitionAsync(input, logger);
        }

        /// <inheritdoc/>
        public async Task MonitorShutdownStateTransitionAsync(string environmentId, Guid computeId, IDiagnosticsLogger logger)
        {
            var input = new EnvironmentStateTransitionInput()
            {
                EnvironmentId = environmentId,
                ComputeResourceId = computeId,
                CurrentState = CloudEnvironmentState.ShuttingDown,
                TargetState = CloudEnvironmentState.Shutdown,
                TransitionTimeout = await EnvironmentMonitorSettings.ShutdownEnvironmentTimeout(logger.NewChildLogger()),
            };

            await MonitorStateTransitionAsync(input, logger);
        }

        /// <inheritdoc/>
        public async Task MonitorQueuedStateTransitionAsync(string environmentId, CloudEnvironmentState targetState, IDiagnosticsLogger logger)
        {
            var input = new EnvironmentStateTransitionInput()
            {
                EnvironmentId = environmentId,
                CurrentState = CloudEnvironmentState.Queued,
                TargetState = targetState,
                TransitionTimeout = await EnvironmentMonitorSettings.QueuedEnvironmentTimeout(logger.NewChildLogger()),
            };

            await MonitorStateTransitionAsync(input, logger);
        }

        /// <inheritdoc/>
        public async Task MonitorUnavailableStateTransitionAsync(string environmentId, Guid computeId, IDiagnosticsLogger logger)
        {
            var input = new EnvironmentStateTransitionInput()
            {
                EnvironmentId = environmentId,
                ComputeResourceId = computeId,
                CurrentState = CloudEnvironmentState.Unavailable,
                TargetState = CloudEnvironmentState.Available,
                TransitionTimeout = await EnvironmentMonitorSettings.UnavailableEnvironmentTimeout(logger.NewChildLogger()),
            };

            await MonitorStateTransitionAsync(input, logger);
        }

        /// <inheritdoc/>
        public async Task MonitorUpdateStateTransitionAsync(string environmentId, Guid computeId, IDiagnosticsLogger logger)
        {
            var input = new EnvironmentStateTransitionInput()
            {
                EnvironmentId = environmentId,
                CurrentState = CloudEnvironmentState.Updating,
                TargetState = CloudEnvironmentState.Shutdown,
                TransitionTimeout = await EnvironmentMonitorSettings.UpdateEnvironmentTimeout(logger.NewChildLogger()),
            };

            await MonitorStateTransitionAsync(input, logger);
        }

        private Task MonitorStateTransitionAsync(EnvironmentStateTransitionInput input, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_monitor_state_transition",
                async (childLogger) =>
                {
                    // Check for flighting switch
                    if (!await EnvironmentMonitorSettings.EnableStateTransitionMonitoring(childLogger.NewChildLogger()))
                    {
                        // Stop environment monitoring
                        return;
                    }

                    var loggingProperties = new Dictionary<string, string>();

                    if (await IsJobContinuationHandlerEnabledAsync(logger))
                    {
                        await JobQueueProducerFactory.GetOrCreate(EnvironmentStateTransitionMonitorJobHandler.DefaultQueueId).AddJobAsync(
                            new EnvironmentStateTransitionMonitorJobHandler.Payload()
                            {
                                CurrentState = input.CurrentState,
                                TargetState = input.TargetState,
                                EnvironmentId = input.EnvironmentId,
                                ComputeResourceId = input.ComputeResourceId,
                            },
                            new JobPayloadOptions() { InitialVisibilityDelay = input.TransitionTimeout },
                            logger,
                            CancellationToken.None);
                    }
                    else
                    {
                        var target = EnvironmentStateTransitionMonitorContinuationHandler.DefaultQueueTarget;

                        var result = await Activator.Execute(target, input, childLogger, null, loggingProperties);
                        if (result.Status != OperationState.InProgress)
                        {
                            throw new EnvironmentMonitorInitializationException(input.EnvironmentId);
                        }
                    }
                });
        }

        private Task<bool> IsJobContinuationHandlerEnabledAsync(IDiagnosticsLogger logger)
        {
            return ConfigurationReader.ReadFeatureFlagAsync("job-continuation-handler", logger, false);
        }
    }
}
