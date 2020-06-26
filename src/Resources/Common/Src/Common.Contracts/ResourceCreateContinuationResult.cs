// <copyright file="ResourceCreateContinuationResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
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

        /// <summary>
        /// Gets or sets the azure resource info for the newly created resource.
        /// </summary>
        public ResourceComponentDetail Components { get; set; }
    }
}
