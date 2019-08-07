// <copyright file="FileShareProviderCreateResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models
{
    /// <summary>
    /// Result of the provider create operation.
    /// </summary>
    public class FileShareProviderCreateResult : BaseContinuationResult
    {
        /// <summary>
        /// Gets or sets the resource id.
        /// </summary>
        public string ResourceId { get; set; }
    }
}
