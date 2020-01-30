// <copyright file="AllocateRequestBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker
{
    /// <summary>
    /// The allocate request body.
    /// </summary>
    public class AllocateRequestBody
    {
        /// <summary>
        /// Gets or sets the cloud environment sku name.
        /// </summary>
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets the resource type to be allocated for this sku.
        /// </summary>
        public ResourceType Type { get; set; }

        /// <summary>
        /// Gets or sets the Azure location for this allocation.
        /// </summary>
        public AzureLocation Location { get; set; }
    }
}
