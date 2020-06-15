// <copyright file="QueueProviderDeleteInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Models
{
    /// <summary>
    /// Queue provider delete input.
    /// </summary>
    public class QueueProviderDeleteInput : ContinuationInput
    {
        /// <summary>
        /// Gets or sets the azure location for the queue.
        /// </summary>
        public AzureLocation Location { get; set; }

        /// <summary>
        /// Gets or sets the azure queue name.
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// Gets or sets the azure resource info.
        /// </summary>
        public AzureResourceInfo AzureResourceInfo { get; set; }
    }
}