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
    /// 
    /// </summary>
    public static class ContinuationTaskActivatorExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="activator"></param>
        /// <param name="input"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static async Task<ContinuationResult> CreatePooledResource(
            this IContinuationTaskActivator activator,
            CreateResourceContinuationInput input,
            IDiagnosticsLogger logger)
        {
            var target = input.Type == ResourceType.ComputeVM ? "JobCreateCompute" : "JobCreateStorage";

            return await activator.Execute(target, input, logger);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="activator"></param>
        /// <param name="input"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static async Task<ContinuationResult> StartComputeResource(
            this IContinuationTaskActivator activator,
            StartEnvironmentContinuationInput input,
            IDiagnosticsLogger logger)
        {
            var target = "JobStartCompute";

            return await activator.Execute(target, input, logger);
        }

        public static async Task<ContinuationResult> DeleteResource(
            this IContinuationTaskActivator activator,
            DeleteResourceContinuationInput input,
            IDiagnosticsLogger logger)
        {
            var target = "JobDeleteResource";

            return await activator.Execute(target, input, logger);
        }
    }
}
