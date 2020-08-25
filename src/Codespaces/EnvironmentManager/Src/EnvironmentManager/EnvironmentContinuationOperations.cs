// <copyright file="EnvironmentContinuationOperations.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;

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
        public EnvironmentContinuationOperations(
            IContinuationTaskActivator activator)
        {
            Activator = activator;
        }

        private IContinuationTaskActivator Activator { get; }

        /// <inheritdoc/>
        public Task<ContinuationResult> ArchiveAsync(
            Guid environmentId,
            DateTime lastStateUpdated,
            string reason,
            IDiagnosticsLogger logger)
        {
            var loggingProperties = BuildLoggingProperties(environmentId, reason);

            var input = new ArchiveEnvironmentContinuationInput()
            {
                EnvironmentId = environmentId,
                LastStateUpdated = lastStateUpdated,
                Reason = reason,
            };
            var target = ArchiveEnvironmentContinuationHandler.DefaultQueueTarget;

            return Activator.Execute(target, input, logger, input.EnvironmentId, loggingProperties);
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

            return Activator.Execute(target, input, logger, input.EnvironmentId, loggingProperties);
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
    }
}
