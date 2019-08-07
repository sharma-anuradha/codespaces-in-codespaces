﻿// <copyright file="FileShareProviderDeleteInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models
{
    /// <summary>
    /// Input for the provider delete operation.
    /// </summary>
    public class FileShareProviderDeleteInput
    {
        /// <summary>
        /// Gets or sets the resource id.
        /// </summary>
        public string ResourceId { get; set; }
    }
}
