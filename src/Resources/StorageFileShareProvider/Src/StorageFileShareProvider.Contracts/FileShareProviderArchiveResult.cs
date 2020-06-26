// <copyright file="FileShareProviderArchiveResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts
{
    /// <summary>
    /// Result of the provider assign operation.
    /// </summary>
    public class FileShareProviderArchiveResult : ContinuationResult
    {
        /// <summary>
        /// Gets or sets the azure resource info.
        /// </summary>
        public AzureResourceInfo AzureResourceInfo { get; set; }
    }
}
