// <copyright file="IResourceNameBuilder.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Resource name builder interface.
    /// </summary>
    public interface IResourceNameBuilder
    {
        /// <summary>
        /// Creates name for cosmos db.
        /// </summary>
        /// <param name="baseName">Basename of the db.</param>
        /// <returns>Name with stamp.</returns>
        string GetCosmosDocDBName(string baseName);

        /// <summary>
        /// Creates a name for queue.
        /// </summary>
        /// <param name="baseName">Basename of the queue.</param>
        /// <returns>Name with stamp.</returns>
        string GetQueueName(string baseName);

        /// <summary>
        /// Creates name for resource group.
        /// </summary>
        /// <param name="baseName">Basename of the resource group.</param>
        /// <returns>Name with stamp.</returns>
        string GetResourceGroupName(string baseName);

        /// <summary>
        /// Creates name for the lease.
        /// </summary>
        /// <param name="baseName"></param>
        /// <returns>Name with stamp.</returns>
        string GetLeaseName(string baseName);
    }
}
