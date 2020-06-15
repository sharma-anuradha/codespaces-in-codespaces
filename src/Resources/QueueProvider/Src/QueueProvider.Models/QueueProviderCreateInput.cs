// <copyright file="QueueProviderCreateInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Models
{
    /// <summary>
    /// Queue provider create input.
    /// </summary>
    public class QueueProviderCreateInput : ContinuationInput
    {
        /// <summary>
        /// Gets or sets the azure location for the queue.
        /// </summary>
        public AzureLocation AzureLocation { get; set; }

        /// <summary>
        /// Gets or sets the azure queue name.
        /// </summary>
        public string QueueName { get; set; }
    }
}