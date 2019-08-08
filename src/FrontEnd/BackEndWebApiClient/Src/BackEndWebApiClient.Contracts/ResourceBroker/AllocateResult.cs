// <copyright file="AllocateResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker
{
    /// <summary>
    /// The result of a resource allocation.
    /// </summary>
    public class AllocateResult
    {
        /// <summary>
        /// Gets or sets the resource id token.
        /// </summary>
        public string ResourceIdToken { get; set; }

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
