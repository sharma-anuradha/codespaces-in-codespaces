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
        /// Gets the control-plane stamp infrastructure resource group name, e.g., vsclk-online-prod-rel-use-infra.
        /// </summary>
        string StampInfrastructureResourceGroupName { get; }

        /// <summary>
        /// Gets the control-plane stamp cosmos db account name.
        /// </summary>
        string StampCosmosDbAccountName { get; }

        /// <summary>
        /// Gets the control-plane stamp storage account name.
        /// </summary>
        string StampStorageAccountName { get; }

        /// <summary>
        /// Gets the control-plane stamp service bus resource group name.
        /// </summary>
        string ServiceBusResourceGroupName { get; }

        /// <summary>
        /// Gets the control-plane stamp service bus namespace name.
        /// </summary>
        string StampServiceBusNamespaceName { get; }

        /// <summary>
        /// Gets the control-plane stamp DNS host name.
        /// </summary>
        string DnsHostName { get; }

        /// <summary>
        /// Gets the control-plane stamp DNS host name.
        /// </summary>
        IEnumerable<AzureLocation> DataPlaneLocations { get; }

        /// <summary>
        /// Gets the Windows image gallery name for an AzureLocation.
        /// </summary>
        /// <param name="azureLocation">The AzureLocation to convert.</param>
        /// <returns>The image gallery name.</returns>
        string GetImageGalleryNameForWindowsImages(AzureLocation azureLocation);

        /// <summary>
        /// Gets the Windows images resource group name for an AzureLocation.
        /// </summary>
        /// <param name="azureLocation">The AzureLocation to convert.</param>
        /// <returns>The images resource group name.</returns>
        string GetResourceGroupNameForWindowsImages(AzureLocation azureLocation);

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
        /// <param name="billingLocation">The billing submission location.</param>
        /// <returns>The storage account name.</returns>
        string GetStampStorageAccountNameForBillingSubmission(AzureLocation billingLocation);

        /// <summary>
        /// Gets the data-plane stamp storage account name for archive storage.
        /// </summary>
        /// <param name="storageLocation">The storage account location.</param>
        /// <param name="index">The index of the storage account. If non null, it is appended in the forma "00".</param>
        /// <returns>The storage account name.</returns>
        string GetDataPlaneStorageAccountNameForArchiveStorageName(AzureLocation storageLocation, int? index = null);
    }
}
