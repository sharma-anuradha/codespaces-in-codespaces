// <copyright file="QueueProviderGetDetailsInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts
{
    /// <summary>
    /// Queue provider get details input.
    /// </summary>
    public class QueueProviderGetDetailsInput
    {
        /// <summary>
        /// Gets or sets the queue name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the azure location.
        /// </summary>
        public AzureLocation AzureLocation { get; set; }
    }
}
