// <copyright file="IControlPlaneStampInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// The control-plane stamp information.
    /// </summary>
    public interface IControlPlaneStampInfo
    {
        /// <summary>
        /// Gets the control-plane stamp azure location.
        /// </summary>
        AzureLocation Location { get; }

        /// <summary>
        /// Gets the control-plane stamp resource group name, e.g., vsclk-online-prod-rel-use.
        /// </summary>
        string StampResourceGroupName { get; }

        /// <summary>
        /// Gets the control-plane stamp cosmos db account name.
        /// </summary>
        string StampCosmosDbAccountName { get; }

        /// <summary>
        /// Gets the control-plane stamp storage  account name.
        /// </summary>
        string StampStorageAccountName { get; }

        /// <summary>
        /// Gets the control-plane stamp DNS host name.
        /// </summary>
        string DnsHostName { get; }

        /// <summary>
        /// Gets the control-plane stamp DNS host name.
        /// </summary>
        IEnumerable<AzureLocation> DataPlaneLocations { get; }

        /// <summary>
        /// Gets the control-plane stamp storage account name for use with compute job queues.
        /// </summary>
        /// <param name="computeVmLocation">The compute vm location.</param>
        /// <returns>The storage account name.</returns>
        string GetStampStorageAccountNameForComputeQueues(AzureLocation computeVmLocation);

        /// <summary>
        /// Gets the control-plane stamp storage account name for vm agent images.
        /// </summary>
        /// <param name="computeVmLocation">The compute vm location.</param>
        /// <returns>The storage account name.</returns>
        string GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation computeVmLocation);

        /// <summary>
        /// Gets the control-plane stamp storage account name for compute storage.
        /// </summary>
        /// <param name="computeStorageLocation">The compute storage location.</param>
        /// <returns>The storage account name.</returns>
        string GetStampStorageAccountNameForStorageImages(AzureLocation computeStorageLocation);

        /// <summary>
        /// Gets the control-plane stamp batch account name.
        /// </summary>
        /// <param name="azureLocation">The data-plane location.</param>
        /// <returns>The batch account name.</returns>
        string GetStampBatchAccountName(AzureLocation azureLocation);

        /// <summary>
        /// Gets the control-plane stamp storage account name for storing billing submissions.
        /// </summary>
        /// <param name="billingLocation">The billing submisisonlocation.</param>
        /// <returns>The storage account name.</returns>
        string GetStampStorageAccountNameForBillingSubmission(AzureLocation billingLocation);
    }
}
