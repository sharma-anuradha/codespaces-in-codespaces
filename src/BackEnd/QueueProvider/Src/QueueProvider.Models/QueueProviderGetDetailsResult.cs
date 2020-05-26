// <copyright file="QueueProviderGetDetailsResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Models
{
    /// <summary>
    /// Queue provider get details result.
    /// </summary>
    public class QueueProviderGetDetailsResult
    {
        /// <summary>
        /// Gets or sets azure resource info.
        /// </summary>
        public AzureResourceInfo AzureResourceInfo { get; set; }
    }
}
