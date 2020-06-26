// <copyright file="FileShareProviderArchiveInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts
{
    /// <summary>
    /// Input for the provider assign operation.
    /// </summary>
    public class FileShareProviderArchiveInput : ContinuationInput
    {
        /// <summary>
        /// Gets or sets the source azure resource info.
        /// </summary>
        public AzureResourceInfo SrcAzureResourceInfo { get; set; }

        /// <summary>
        /// Gets or sets the file share src url.
        /// </summary>
        public string SrcFileShareUriWithSas { get; set; }

        /// <summary>
        /// Gets or sets the blob dest url.
        /// </summary>
        public string DestBlobUriWithSas { get; set; }
    }
}
