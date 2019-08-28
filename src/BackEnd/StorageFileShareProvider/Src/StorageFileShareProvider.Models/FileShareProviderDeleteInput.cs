// <copyright file="FileShareProviderDeleteInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models
{
    /// <summary>
    /// Input for the provider delete operation.
    /// </summary>
    public class FileShareProviderDeleteInput
    {
        /// <summary>
        /// Gets or sets the azure resource info.
        /// </summary>
        public AzureResourceInfo AzureResourceInfo { get; set; }
    }
}
