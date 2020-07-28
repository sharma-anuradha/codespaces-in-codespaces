// <copyright file="IResourceContinuationOperations.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

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
        /// <param name="loggingProperties">The dictionary of logging properties.</param>
        /// <returns>Resuling continuation result.</returns>
        Task<ContinuationResult> CreateAsync(
            Guid resourceId,
            ResourceType type,
            ResourcePoolResourceDetails details,
            string reason,
            IDiagnosticsLogger logger,
            IDictionary<string, string> loggingProperties = null);

        /// <summary>
        /// Starts environment by invoking the continution activator.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="computeResourceId">Target compute resource id.</param>
        /// <param name="osDiskResourceId">Target osdisk resource id.</param>
        /// <param name="storageResourceId">Target storage resource id.</param>
        /// <param name="archiveStorageResourceId">Target blob storage resource id.</param>
        /// <param name="environmentVariables">Input environment variables for the compute.</param>
        /// <param name="userSecrets">User secrets applicable to the environment.</param>
        /// <param name="reason">Trigger for operation.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="loggingProperties">The dictionary of logging properties.</param>
        /// <returns>Resuling continuation result.</returns>
        Task<ContinuationResult> StartEnvironmentAsync(
            Guid environmentId,
            Guid computeResourceId,
            Guid? osDiskResourceId,
            Guid? storageResourceId,
            Guid? archiveStorageResourceId,
            IDictionary<string, string> environmentVariables,
            IEnumerable<UserSecretData> userSecrets,
            string reason,
            IDiagnosticsLogger logger,
            IDictionary<string, string> loggingProperties = null);

        /// <summary>
        /// Starts arhive of storage by invoking the continution activator.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="blobResourceId">Target blob resource id.</param>
        /// <param name="storageResourceId">Target storage resource id.</param>
        /// <param name="reason">Trigger for operation.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="loggingProperties">The dictionary of logging properties.</param>
        /// <returns>Resuling continuation result.</returns>
        Task<ContinuationResult> StartArchiveAsync(
            Guid environmentId,
            Guid blobResourceId,
            Guid storageResourceId,
            string reason,
            IDiagnosticsLogger logger,
            IDictionary<string, string> loggingProperties = null);

        /// <summary>
        /// Delete resource by invoking the continution activator.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="resourceId">Target resource id.</param>
        /// <param name="reason">Trigger for operation.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="loggingProperties">The dictionary of logging properties.</param>
        /// <returns>Resuling continuation result.</returns>
        Task<ContinuationResult> DeleteAsync(
            Guid? environmentId,
            Guid resourceId,
            string reason,
            IDiagnosticsLogger logger,
            IDictionary<string, string> loggingProperties = null);

        /// <summary>
        /// Delete resource by invoking the continution activator.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="resourceId">Target resource id.</param>
        /// <param name="reason">Trigger for operation.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="loggingProperties">The dictionary of logging properties.</param>
        /// <returns>Resuling continuation result.</returns>
        Task<ContinuationResult> SuspendAsync(
            Guid environmentId,
            Guid resourceId,
            string reason,
            IDiagnosticsLogger logger,
            IDictionary<string, string> loggingProperties = null);

        /// <summary>
        /// Delete orphaned resource by invoking the continuation activator.
        /// </summary>
        /// <param name="resourceId">Target resource id.</param>
        /// <param name="subscriptionId">The azure subscription id.</param>
        /// <param name="resourceGroup">The azure resource group.</param>
        /// <param name="name">The resource name.</param>
        /// <param name="azureLocation">Azure location.</param>
        /// <param name="resourceTags">Azure resource tags.</param>
        /// <param name="resourceType">Resource type.</param>
        /// <param name="reason">Trigger for operation.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Resulting continuation result.</returns>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<ContinuationResult> DeleteOrphanedResourceAsync(
            Guid resourceId,
            Guid subscriptionId,
            string resourceGroup,
            string name,
            AzureLocation azureLocation,
            IDictionary<string, string> resourceTags,
            ResourceType resourceType,
            string reason,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Create custom resources.
        /// </summary>
        /// <param name="resourceId">resource id.</param>
        /// <param name="type">resource type.</param>
        /// <param name="extendedProperties">extended properties.</param>
        /// <param name="details">pool details.</param>
        /// <param name="reason">reason.</param>
        /// <param name="logger">logger.</param>
        /// <param name="loggingProperties">The dictionary of logging properties.</param>
        /// <returns>result.</returns>
        Task<ResourceRecord> QueueCreateAsync(
            Guid resourceId,
            ResourceType type,
            AllocateExtendedProperties extendedProperties,
            ResourcePoolResourceDetails details,
            string reason,
            IDiagnosticsLogger logger,
            IDictionary<string, string> loggingProperties = null);
    }
}
