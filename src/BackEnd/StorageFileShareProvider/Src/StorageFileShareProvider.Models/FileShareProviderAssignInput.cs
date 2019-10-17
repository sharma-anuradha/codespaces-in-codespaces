// <copyright file="FileShareProviderAssignInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models
{
    /// <summary>
    /// Input for the provider assign operation.
    /// </summary>
    public class FileShareProviderAssignInput : ContinuationInput
    {
        /// <summary>
        /// Gets or sets the azure resource info.
        /// </summary>
        public AzureResourceInfo AzureResourceInfo { get; set; }

        /// <summary>
        /// Gets or sets the type of storage being assigned.
        /// </summary>
        public StorageType StorageType { get; set; }
    }
}
