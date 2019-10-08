// <copyright file="ContinuationTaskActivatorExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

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
        /// <param name="resourceId">Target resource id.</param>
        /// <param name="type">Target type.</param>
        /// <param name="detials">Target detials.</param>
        /// <param name="trigger">Trigger for operation.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Resuling continuation result.</returns>
        public static async Task<ContinuationResult> CreateResource(
            this IContinuationTaskActivator activator,
            Guid resourceId,
            ResourceType type,
            ResourcePoolResourceDetails detials,
            string trigger,
            IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("ResourceId", resourceId);

            var input = new CreateResourceContinuationInput()
            {
                Type = type,
                ResourcePoolDetails = detials,
                ResourceId = resourceId,
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
        /// <param name="environmentVariables">Input environment variables for the compute.</param>
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
            logger.FluentAddBaseValue("ResourceId", computeResourceId)
                .FluentAddBaseValue("StorageResourceId", storageResourceId);

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
            logger.FluentAddBaseValue("ResourceId", resourceId);

            var input = new DeleteResourceContinuationInput()
            {
                ResourceId = resourceId,
                Reason = trigger,
            };
            var target = DeleteResourceContinuationHandler.DefaultQueueTarget;

            return await activator.Execute(target, input, logger, input.ResourceId);
        }

        /// <summary>
        /// Delete orphaned compute resource by invoking the continution activator.
        /// </summary>
        /// <param name="activator">Target continuation activator.</param>
        /// <param name="resourceId">Target resource id.</param>
        /// <param name="subscriptionId">The azure subscription id.</param>
        /// <param name="resourceGroup">The azure resource group.</param>
        /// <param name="name">The resource name.</param>
        /// <param name="location">The resource location.</param>
        /// <param name="resourceTags">Azure resource tags.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Resuling continuation result.</returns>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<ContinuationResult> DeleteOrphanedComputeAsync(
            this IContinuationTaskActivator activator,
            Guid resourceId,
            Guid subscriptionId,
            string resourceGroup,
            string name,
            AzureLocation location,
            IDictionary<string, string> resourceTags,
            IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("ResourceId", resourceId);

            var input = new VirtualMachineProviderDeleteInput()
            {
                 AzureVmLocation = location,
                 AzureResourceInfo = new AzureResourceInfo(subscriptionId, resourceGroup, name),
            };
            if (resourceTags != null && resourceTags.ContainsKey(ResourceTagName.ComputeOS))
            {
                if (!Enum.TryParse(resourceTags[ResourceTagName.ComputeOS], true, out ComputeOS computeOS))
                {
                    throw new NotSupportedException($"Resource has a compute OS of {resourceTags[ResourceTagName.ComputeOS]} which is not supported");
                }

                input.ComputeOS = computeOS;
            }

            var target = DeleteOrphanedResourceContinuationHandler.DefaultQueueTarget;

            return await activator.Execute(target, input, logger, resourceId);
        }

        /// <summary>
        /// Delete orphaned storage resource by invoking the continuation activator.
        /// </summary>
        /// <param name="activator">Target continuation activator.</param>
        /// <param name="resourceId">Target resource id.</param>
        /// <param name="subscriptionId">The azure subscription id.</param>
        /// <param name="resourceGroup">The azure resource group.</param>
        /// <param name="name">The resource name.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Resulting continuation result.</returns>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<ContinuationResult> DeleteOrphanedStorageAsync(
            this IContinuationTaskActivator activator,
            Guid resourceId,
            Guid subscriptionId,
            string resourceGroup,
            string name,
            IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("ResourceId", resourceId);

            var input = new FileShareProviderDeleteInput()
            {
                AzureResourceInfo = new AzureResourceInfo(subscriptionId, resourceGroup, name),
            };
            var target = DeleteOrphanedResourceContinuationHandler.DefaultQueueTarget;

            return await activator.Execute(target, input, logger, resourceId);
        }
    }
}
