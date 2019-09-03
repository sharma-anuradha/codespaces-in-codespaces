// <copyright file="ContinuationTaskActivatorExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;

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
        /// <param name="input">Input to be passed into the contination activator.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Resuling continuation result.</returns>
        public static async Task<ContinuationResult> CreateResource(
            this IContinuationTaskActivator activator,
            CreateResourceContinuationInput input,
            IDiagnosticsLogger logger)
        {
            var target = CreateResourceContinuationHandler.DefaultQueueTarget;

            return await activator.Execute(target, input, logger);
        }

        /// <summary>
        /// Starts environment by invoking the continution activator.
        /// </summary>
        /// <param name="activator">Target continuation activator.</param>
        /// <param name="input">Input to be passed into the contination activator.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Resuling continuation result.</returns>
        public static async Task<ContinuationResult> StartEnvironment(
            this IContinuationTaskActivator activator,
            StartEnvironmentContinuationInput input,
            IDiagnosticsLogger logger)
        {
            var target = StartEnvironmentContinuationHandler.DefaultQueueTarget;

            return await activator.Execute(target, input, logger);
        }

        /// <summary>
        /// Delete resource by invoking the continution activator.
        /// </summary>
        /// <param name="activator">Target continuation activator.</param>
        /// <param name="input">Input to be passed into the contination activator.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Resuling continuation result.</returns>
        public static async Task<ContinuationResult> DeleteResource(
            this IContinuationTaskActivator activator,
            DeleteResourceContinuationInput input,
            IDiagnosticsLogger logger)
        {
            var target = DeleteResourceContinuationHandler.DefaultQueueTarget;

            return await activator.Execute(target, input, logger);
        }
    }
}
