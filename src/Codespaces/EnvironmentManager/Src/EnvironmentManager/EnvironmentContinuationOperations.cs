// <copyright file="EnvironmentContinuationOperations.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kusto.Cloud.Platform.Utils;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Environment Continuation Operations.
    /// </summary>
    public class EnvironmentContinuationOperations : IEnvironmentContinuationOperations
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentContinuationOperations"/> class.
        /// </summary>
        /// <param name="activator">Target activator.</param>
        /// <param name="resourceRepository">Target resource repository.</param>
        /// <param name="jobQueueProducerFactory">Job Queue producer factory instance.</param>
        /// <param name="configurationReader">Configuration reader.</param>
        public EnvironmentContinuationOperations(
            IContinuationTaskActivator activator,
            IJobQueueProducerFactory jobQueueProducerFactory,
            IConfigurationReader configurationReader)
        {
            Activator = activator;
            JobQueueProducerFactory = jobQueueProducerFactory;
            ConfigurationReader = configurationReader;
        }

        private IContinuationTaskActivator Activator { get; }

        private IJobQueueProducerFactory JobQueueProducerFactory { get; }

        private IConfigurationReader ConfigurationReader { get; }

        /// <inheritdoc/>
        public async Task<ContinuationResult> ArchiveAsync(
            Guid environmentId,
            DateTime lastStateUpdated,
            string reason,
            IDiagnosticsLogger logger)
        {
            var loggingProperties = BuildLoggingProperties(environmentId, reason);
            if (await IsJobContinuationHandlerEnabledAsync(logger))
            {
                await JobQueueProducerFactory.GetOrCreate(ArchiveEnvironmentContinuationJobHandler.DefaultQueueId).AddJobContinuationAsync(
                    new ArchiveEnvironmentContinuationJobHandler.ArchiveContinuationInput()
                    {
                        EntityId = environmentId,
                        LastStateUpdated = lastStateUpdated,
                        Reason = reason,
                        LoggerProperties = loggingProperties.CreateLoggerProperties(),
                    },
                    null,
                    logger,
                    CancellationToken.None);
                return null;
            }
            else
            {
                var input = new ArchiveEnvironmentContinuationInput()
                {
                    EnvironmentId = environmentId,
                    LastStateUpdated = lastStateUpdated,
                    Reason = reason,
                };
                var target = ArchiveEnvironmentContinuationHandler.DefaultQueueTarget;

                return await Activator.Execute(target, input, logger, input.EnvironmentId, loggingProperties);
            }
        }

        /// <inheritdoc/>
        public async Task CreatePoolResourceAsync(
            string skuName,
            AzureLocation location,
            string reason,
            IDiagnosticsLogger logger)
        {
            var environmentId = Guid.NewGuid();
            var loggingProperties = BuildLoggingProperties(environmentId, reason);

            await JobQueueProducerFactory
                .GetOrCreate(CreateEnvironmentResourceJobHandler.DefaultQueueId)
                .AddJobContinuationAsync(
                new CreateEnvironmentResourceJobHandler.Payload()
                {
                    EntityId = environmentId,
                    SkuName = skuName,
                    Location = location,
                    Reason = reason,
                    LoggerProperties = loggingProperties.CreateLoggerProperties(),
                    CurrentState = CreateEnvironmentResourceJobHandler.JobState.AllocateResource,
                },
                null,
                logger,
                CancellationToken.None);
        }

        /// <inheritdoc/>
        public Task<ContinuationResult> CreateAsync(
            Guid environmentId,
            DateTime lastStateUpdated,
            CloudEnvironmentOptions cloudEnvironmentOptions,
            StartCloudEnvironmentParameters startCloudEnvironmentParameters,
            string reason,
            IDiagnosticsLogger logger)
        {
            return StartEnvironmentContinuationAsync(environmentId, lastStateUpdated, cloudEnvironmentOptions, startCloudEnvironmentParameters, reason, StartEnvironmentInputActionState.CreateNew, logger);
        }

        /// <inheritdoc/>
        public Task<ContinuationResult> ResumeAsync(
            Guid environmentId,
            DateTime lastStateUpdated,
            StartCloudEnvironmentParameters startCloudEnvironmentParameters,
            string reason,
            IDiagnosticsLogger logger)
        {
            return StartEnvironmentContinuationAsync(environmentId, lastStateUpdated, null, startCloudEnvironmentParameters, reason, StartEnvironmentInputActionState.Resume, logger);
        }

        /// <inheritdoc/>
        public async Task<ContinuationResult> ShutdownAsync(
            Guid environmentId,
            bool forceSuspend,
            string reason,
            IDiagnosticsLogger logger)
        {
            var loggingProperties = BuildLoggingProperties(environmentId, reason);

            if (await IsJobContinuationHandlerEnabledAsync(logger))
            {
                await JobQueueProducerFactory.GetOrCreate(ShutdownEnvironmentContinuationJobHandler.DefaultQueueId).AddJobContinuationAsync(
                    new ShutdownEnvironmentContinuationJobHandler.ShutdownEnvironmentContinuationInput()
                    {
                        EntityId = environmentId,
                        Reason = reason,
                        Force = forceSuspend,
                        LoggerProperties = loggingProperties.CreateLoggerProperties(),
                    },
                    null,
                    logger,
                    CancellationToken.None);
                return null;
            }
            else
            {
                var input = new ShutdownEnvironmentContinuationInput()
                {
                    EnvironmentId = environmentId,
                    Reason = reason,
                    Force = forceSuspend,
                };

                var target = ShutdownEnvironmentContinuationHandler.DefaultQueueTarget;
                return await Activator.Execute(target, input, logger, input.EnvironmentId, loggingProperties);
            }
        }

        /// <inheritdoc/>
        public Task<ContinuationResult> ExportAsync(
            Guid environmentId,
            DateTime lastStateUpdated,
            ExportCloudEnvironmentParameters exportCloudEnvironmentParameters,
            string reason,
            IDiagnosticsLogger logger)
        {
            return StartEnvironmentContinuationAsync(environmentId, lastStateUpdated, null, exportCloudEnvironmentParameters, reason, StartEnvironmentInputActionState.Export, logger);
        }

        private async Task<ContinuationResult> StartEnvironmentContinuationAsync(
            Guid environmentId,
            DateTime lastStateUpdated,
            CloudEnvironmentOptions cloudEnvironmentOptions,
            CloudEnvironmentParameters cloudEnvironmentParameters,
            string reason,
            StartEnvironmentInputActionState actionState,
            IDiagnosticsLogger logger)
        {
            var loggingProperties = BuildLoggingProperties(environmentId, reason);

            if (await IsJobContinuationHandlerEnabledAsync(logger))
            {
                await JobQueueProducerFactory.GetOrCreate(StartEnvironmentContinuationJobHandlerV2.DefaultQueueId).AddJobContinuationAsync(
                    new StartEnvironmentContinuationJobHandlerV2.StartEnvironmentContinuationInput()
                    {
                        CloudEnvironmentOptions = cloudEnvironmentOptions,
                        ActionState = actionState,
                        EntityId = environmentId,
                        LastStateUpdated = lastStateUpdated,
                        CloudEnvironmentParameters = cloudEnvironmentParameters,
                        Reason = reason,
                        LoggerProperties = loggingProperties.CreateLoggerProperties(),
                        CurrentState = StartEnvironmentContinuationInputState.StartQueuedStateMonitor,
                    },
                    null,
                    logger,
                    CancellationToken.None);
                return null;
            }
            else
            {
                var input = new StartEnvironmentContinuationInputV2()
                {
                    EnvironmentId = environmentId,
                    LastStateUpdated = lastStateUpdated,
                    CloudEnvironmentParameters = cloudEnvironmentParameters,
                    Reason = reason,
                    ActionState = actionState,
                    CurrentState = StartEnvironmentContinuationInputState.StartQueuedStateMonitor,
                };

                var target = StartEnvironmentContinuationHandlerV2.DefaultQueueTarget;

                return await Activator.Execute(target, input, logger, input.EnvironmentId, loggingProperties);
            }
        }

        /// <inheritdoc/>
        public Task<ContinuationResult> UpdateSystemAsync(
            Guid environmentId,
            DateTime lastStateUpdated,
            CloudEnvironmentParameters cloudEnvironmentParams,
            string reason,
            IDiagnosticsLogger logger)
        {
            return StartEnvironmentContinuationAsync(
                environmentId,
                lastStateUpdated,
                null,
                cloudEnvironmentParams,
                reason,
                StartEnvironmentInputActionState.Update,
                logger);
        }

        private IDictionary<string, string> BuildLoggingProperties(
            Guid resourceId,
            string reason)
        {
            return new Dictionary<string, string>()
                {
                    { EnvironmentLoggingPropertyConstants.EnvironmentId, resourceId.ToString() },
                    { EnvironmentLoggingPropertyConstants.OperationReason, reason },
                };
        }

        private Task<bool> IsJobContinuationHandlerEnabledAsync(IDiagnosticsLogger logger)
        {
            return ConfigurationReader.ReadFeatureFlagAsync("job-continuation-handler", logger, true);
        }
    }
}
