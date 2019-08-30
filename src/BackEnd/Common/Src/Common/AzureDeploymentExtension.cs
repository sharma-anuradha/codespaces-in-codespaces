// <copyright file="AzureDeploymentExtension.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common
{
    /// <summary>
    /// Azure deployment related utilities.
    /// </summary>
    public static class AzureDeploymentExtension
    {
        /// <summary>
        /// Create resource group if not exists.
        /// </summary>
        /// <param name="azure">azure client provider.</param>
        /// <param name="subscriptionId">azure subscription id.</param>
        /// <param name="resourceGroupName">azure resource group name.</param>
        /// <param name="location">azure location where resource group will be created.</param>
        /// <returns>Task.</returns>
        public static async Task CreateIfNotExistsResourceGroupAsync(this IAzure azure, string resourceGroupName, string location)
        {
            if (await azure.ResourceGroups.ContainAsync(resourceGroupName))
            {
                return;
            }

            await azure.ResourceGroups.Define(resourceGroupName)
                  .WithRegion(location)
                  .CreateAsync();
        }

        /// <summary>
        /// Delete Azure Resource group.
        /// </summary>
        /// <param name="azure">azure client provider.</param>
        /// <param name="subscriptionId">azure subscription id.</param>
        /// <param name="resourceGroupName">azure resource group name.</param>
        /// <returns>Task.</returns>
        public static async Task DeleteResourceGroupAsync(this IAzure azure, string resourceGroupName)
        {
            await azure.ResourceGroups
            .BeginDeleteByNameAsync(resourceGroupName);
        }
    }
}