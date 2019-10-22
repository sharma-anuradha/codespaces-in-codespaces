// <copyright file="IResourceContinuationOperations.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Resource continuation operations to make involving/starting specific handlers
    /// easier.
    /// </summary>
    public interface IResourceContinuationOperations
    {
        /// <summary>
        /// Create compute resource by invoking the continution activator.
        /// </summary>
        /// <param name="resourceId">Target resource id.</param>
        /// <param name="type">Target type.</param>
        /// <param name="details">Target details.</param>
        /// <param name="reason">Trigger for operation.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Resuling continuation result.</returns>
        Task<ContinuationResult> CreateResource(
            Guid resourceId,
            ResourceType type,
            ResourcePoolResourceDetails details,
            string reason,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Starts environment by invoking the continution activator.
        /// </summary>
        /// <param name="computeResourceId">Target compute resource id.</param>
        /// <param name="storageResourceId">Target storage resource id.</param>
        /// <param name="environmentVariables">Input environment variables for the compute.</param>
        /// <param name="reason">Trigger for operation.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Resuling continuation result.</returns>
        Task<ContinuationResult> StartEnvironment(
            Guid computeResourceId,
            Guid storageResourceId,
            IDictionary<string, string> environmentVariables,
            string reason,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Delete resource by invoking the continution activator.
        /// </summary>
        /// <param name="resourceId">Target resource id.</param>
        /// <param name="reason">Trigger for operation.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Resuling continuation result.</returns>
        Task<ContinuationResult> DeleteResource(
            Guid resourceId,
            string reason,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Delete resource by invoking the continution activator.
        /// </summary>
        /// <param name="resourceId">Target resource id.</param>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="reason">Trigger for operation.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Resuling continuation result.</returns>
        Task<ContinuationResult> CleanupResource(
            Guid resourceId,
            string environmentId,
            string reason,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Delete orphaned compute resource by invoking the continution activator.
        /// </summary>
        /// <param name="resourceId">Target resource id.</param>
        /// <param name="subscriptionId">The azure subscription id.</param>
        /// <param name="resourceGroup">The azure resource group.</param>
        /// <param name="name">The resource name.</param>
        /// <param name="location">The resource location.</param>
        /// <param name="resourceTags">Azure resource tags.</param>
        /// <param name="reason">Trigger for operation.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Resuling continuation result.</returns>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<ContinuationResult> DeleteOrphanedComputeAsync(
            Guid resourceId,
            Guid subscriptionId,
            string resourceGroup,
            string name,
            AzureLocation location,
            IDictionary<string, string> resourceTags,
            string reason,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Delete orphaned storage resource by invoking the continuation activator.
        /// </summary>
        /// <param name="resourceId">Target resource id.</param>
        /// <param name="subscriptionId">The azure subscription id.</param>
        /// <param name="resourceGroup">The azure resource group.</param>
        /// <param name="name">The resource name.</param>
        /// <param name="location">The resource location.</param>
        /// <param name="resourceTags">Azure resource tags.</param>
        /// <param name="reason">Trigger for operation.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Resulting continuation result.</returns>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<ContinuationResult> DeleteOrphanedStorageAsync(
            Guid resourceId,
            Guid subscriptionId,
            string resourceGroup,
            string name,
            AzureLocation location,
            IDictionary<string, string> resourceTags,
            string reason,
            IDiagnosticsLogger logger);
    }
}
