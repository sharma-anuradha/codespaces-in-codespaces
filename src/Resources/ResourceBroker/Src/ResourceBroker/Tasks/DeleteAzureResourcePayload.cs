// <copyright file="DeleteAzureResourcePayload.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// A delete one Azure resource payload.
    /// </summary>
    public class DeleteAzureResourcePayload : JobPayload
    {
        /// <summary>
        /// Gets or sets the subscription id.
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the Azure resource.
        /// </summary>
        public GenericResourceInner AzureResource { get; set; }
    }
}
