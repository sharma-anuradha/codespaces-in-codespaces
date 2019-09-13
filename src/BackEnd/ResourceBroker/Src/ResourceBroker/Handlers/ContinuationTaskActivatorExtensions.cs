// <copyright file="ContinuationTaskActivatorExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Continuation activator extensions to make involing/starting specific handlers
    /// easier.
    /// </summary>
    public static class ContinuationTaskActivatorExtensions
    {
        /// <summary>
        /// Create compute resource by invoking the continution activator.
        /// </summary>
        /// <param name="activator">Target continuation activator.</param>
        /// <param name="type">Target type.</param>
        /// <param name="detials">Target detials.</param>
        /// <param name="trigger">Trigger for operation.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Resuling continuation result.</returns>
        public static async Task<ContinuationResult> CreateResource(
            this IContinuationTaskActivator activator,
            ResourceType type,
            ResourcePoolResourceDetails detials,
            string trigger,
            IDiagnosticsLogger logger)
        {

            var input = new CreateResourceContinuationInput()
            {
                Type = type,
                ResourcePoolDetails = detials,
                ResourceId = Guid.NewGuid(),
                Reason = trigger,
            };
            var target = CreateResourceContinuationHandler.DefaultQueueTarget;

            return await activator.Execute(target, input, logger, input.ResourceId);
        }

        /// <summary>
        /// Starts environment by invoking the continution activator.
        /// </summary>
        /// <param name="activator">Target continuation activator.</param>
        /// <param name="computeResourceId">Target compute resource id.</param>
        /// <param name="storageResourceId">Target storage resource id.</param>
        /// <param name="environmentVariables">Input environement variables for the compute.</param>
        /// <param name="trigger">Trigger for operation.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Resuling continuation result.</returns>
        public static async Task<ContinuationResult> StartEnvironment(
            this IContinuationTaskActivator activator,
            Guid computeResourceId,
            Guid storageResourceId,
            IDictionary<string, string> environmentVariables,
            string trigger,
            IDiagnosticsLogger logger)
        {
            var input = new StartEnvironmentContinuationInput()
            {
                ResourceId = computeResourceId,
                StorageResourceId = storageResourceId,
                EnvironmentVariables = environmentVariables,
                Reason = trigger,
            };
            var target = StartEnvironmentContinuationHandler.DefaultQueueTarget;

            return await activator.Execute(target, input, logger, input.ResourceId);
        }

        /// <summary>
        /// Delete resource by invoking the continution activator.
        /// </summary>
        /// <param name="activator">Target continuation activator.</param>
        /// <param name="resourceId">Target resource id.</param>
        /// <param name="trigger">Trigger for operation.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Resuling continuation result.</returns>
        public static async Task<ContinuationResult> DeleteResource(
            this IContinuationTaskActivator activator,
            Guid resourceId,
            string trigger,
            IDiagnosticsLogger logger)
        {
            var input = new DeleteResourceContinuationInput()
            {
                ResourceId = resourceId,
                Reason = trigger,
            };
            var target = DeleteResourceContinuationHandler.DefaultQueueTarget;

            return await activator.Execute(target, input, logger, input.ResourceId);
        }
    }
}
