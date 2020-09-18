// <copyright file="EnvironmentContinuationOperations.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;

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
            if (await IsJobContinuationHandlerEnabledAsync(logger))
            {
                await JobQueueProducerFactory.GetOrCreate(ArchiveEnvironmentContinuationJobHandler.DefaultQueueId).AddJobAsync(
                    new ArchiveEnvironmentContinuationJobHandler.ArchiveContinuationInput()
                    {
                        EnvironmentId = environmentId,
                        LastStateUpdated = lastStateUpdated,
                        Reason = reason,
                    },
                    null,
                    logger,
                    CancellationToken.None);
                return null;
            }
            else
            {
                var loggingProperties = BuildLoggingProperties(environmentId, reason);

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
        public async Task<ContinuationResult> CreateAsync(
            Guid environmentId,
            DateTime lastStateUpdated,
            CloudEnvironmentOptions cloudEnvironmentOptions,
            StartCloudEnvironmentParameters startCloudEnvironmentParameters,
            string reason,
            IDiagnosticsLogger logger)
        {
            if (await IsJobContinuationHandlerEnabledAsync(logger))
            {
                await JobQueueProducerFactory.GetOrCreate(StartEnvironmentContinuationJobHandlerV2.DefaultQueueId).AddJobAsync(
                    new StartEnvironmentContinuationJobHandlerV2.StartEnvironmentContinuationInput()
                    {
                        CloudEnvironmentOptions = cloudEnvironmentOptions,
                        ActionState = StartEnvironmentInputActionState.CreateNew,
                        EnvironmentId = environmentId,
                        LastStateUpdated = lastStateUpdated,
                        CloudEnvironmentParameters = startCloudEnvironmentParameters,
                        Reason = reason,
                    },
                    null,
                    logger,
                    CancellationToken.None);
                return null;
            }
            else
            {
                var loggingProperties = BuildLoggingProperties(environmentId, reason);

                var input = new StartEnvironmentContinuationInputV2()
                {
                    EnvironmentId = environmentId,
                    LastStateUpdated = lastStateUpdated,
                    CloudEnvironmentParameters = startCloudEnvironmentParameters,
                    Reason = reason,
                    ActionState = StartEnvironmentInputActionState.CreateNew,
                };

                var target = StartEnvironmentContinuationHandlerV2.DefaultQueueTarget;

                return await Activator.Execute(target, input, logger, input.EnvironmentId, loggingProperties);
            }
        }

        /// <inheritdoc/>
        public Task<ContinuationResult> ResumeAsync(
            Guid environmentId,
            DateTime lastStateUpdated,
            StartCloudEnvironmentParameters startCloudEnvironmentParameters,
            string reason,
            IDiagnosticsLogger logger)
        {
            var loggingProperties = BuildLoggingProperties(environmentId, reason);

            var input = new StartEnvironmentContinuationInputV2()
            {
                EnvironmentId = environmentId,
                LastStateUpdated = lastStateUpdated,
                CloudEnvironmentParameters = startCloudEnvironmentParameters,
                Reason = reason,
                ActionState = StartEnvironmentInputActionState.Resume,
            };

            var target = StartEnvironmentContinuationHandlerV2.DefaultQueueTarget;

            return Activator.Execute(target, input, logger, input.EnvironmentId, loggingProperties);
        }

        /// <inheritdoc/>
        public Task<ContinuationResult> ShutdownAsync(
            Guid environmentId,
            bool forceSuspend,
            string reason,
            IDiagnosticsLogger logger)
        {
            var loggingProperties = BuildLoggingProperties(environmentId, reason);

            var input = new ShutdownEnvironmentContinuationInput()
            {
                EnvironmentId = environmentId,
                Reason = reason,
                Force = forceSuspend,
            };

            var target = ShutdownEnvironmentContinuationHandler.DefaultQueueTarget;

            return Activator.Execute(target, input, logger, input.EnvironmentId, loggingProperties);
        }

        /// <inheritdoc/>
        public Task<ContinuationResult> ExportAsync(
            Guid environmentId,
            DateTime lastStateUpdated,
            ExportCloudEnvironmentParameters exportCloudEnvironmentParameters,
            string reason,
            IDiagnosticsLogger logger)
        {
            var loggingProperties = BuildLoggingProperties(environmentId, reason);

            var input = new StartEnvironmentContinuationInputV2()
            {
                EnvironmentId = environmentId,
                LastStateUpdated = lastStateUpdated,
                CloudEnvironmentParameters = exportCloudEnvironmentParameters,
                Reason = reason,
                ActionState = StartEnvironmentInputActionState.Export,
            };

            var target = StartEnvironmentContinuationHandlerV2.DefaultQueueTarget;

            return Activator.Execute(target, input, logger, input.EnvironmentId, loggingProperties);
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
            return ConfigurationReader.ReadFeatureFlagAsync("job-continuation-handler", logger, false);
        }
    }
}
