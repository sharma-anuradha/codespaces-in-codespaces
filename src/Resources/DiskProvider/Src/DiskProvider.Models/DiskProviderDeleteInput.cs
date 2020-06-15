// <copyright file="DiskProviderDeleteInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Models
{
    /// <summary>
    /// Provides input to delete the operating system disk.
    /// </summary>
    public class DiskProviderDeleteInput : ContinuationInput
    {
        /// <summary>
        /// Gets or sets the azure resource to be deleted.
        /// </summary>
        public AzureResourceInfo AzureResourceInfo { get; set; }

        /// <summary>
        /// Gets or sets the queue resource info which needs to be deleted.
        /// </summary>
        public AzureResourceInfo QueueResourceInfo { get; set; }
    }
}
