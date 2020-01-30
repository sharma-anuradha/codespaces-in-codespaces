// <copyright file="AllocateResponseBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker
{
    /// <summary>
    /// The allocate request body.
    /// </summary>
    public class AllocateResponseBody
    {
        /// <summary>
        /// Gets or sets the resource id token.
        /// </summary>
        public Guid ResourceId { get; set; }

        /// <summary>
        /// Gets or sets the cloud environment sku name.
        /// </summary>
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets the resource type that has been allocated.
        /// </summary>
        public ResourceType Type { get; set; }

        /// <summary>
        /// Gets or sets the Azure location where the resource has been allocated.
        /// </summary>
        public AzureLocation Location { get; set; }

        /// <summary>
        /// Gets or sets the resource allocation created timestamp.
        /// </summary>
        public DateTime Created { get; set; }
    }
}
