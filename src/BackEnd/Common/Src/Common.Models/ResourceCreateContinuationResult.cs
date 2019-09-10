﻿// <copyright file="ResourceCreateContinuationResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models
{
    /// <summary>
    /// Identifies the azure resource that was created.
    /// </summary>
    public class ResourceCreateContinuationResult : ContinuationResult
    {
        /// <summary>
        /// Gets or sets the azure resource info for the newly created resource.
        /// </summary>
        public AzureResourceInfo AzureResourceInfo { get; set; }
    }
}
