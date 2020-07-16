// <copyright file="IResourceStartManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Manages resource start.
    /// </summary>
    public interface IResourceStartManager
    {
        /// <summary>
        /// Start Compute.
        /// </summary>
        /// <param name="cloudEnvironment">cloud environment.</param>
        /// <param name="computeResourceId">compute resource id.</param>
        /// <param name="osDiskResourceId">os disk resource id.</param>
        /// <param name="storageResourceId">storage resource id.</param>
        /// <param name="archiveStorageResourceId">archive storage id.</param>
        /// <param name="cloudEnvironmentOptions">cloud environment options.</param>
        /// <param name="startCloudEnvironmentParameters">start environment params.</param>
        /// <param name="logger">logger.</param>
        /// <returns>resule.</returns>
        Task<bool> StartComputeAsync(
           CloudEnvironment cloudEnvironment,
           Guid computeResourceId,
           Guid? osDiskResourceId,
           Guid? storageResourceId,
           Guid? archiveStorageResourceId,
           CloudEnvironmentOptions cloudEnvironmentOptions,
           StartCloudEnvironmentParameters startCloudEnvironmentParameters,
           IDiagnosticsLogger logger);
    }
}