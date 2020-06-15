// <copyright file="AllocateInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models
{
    /// <summary>
    /// Input required for Allocation requests.
    /// </summary>
    public class AllocateInput
    {
        /// <summary>
        /// Gets or sets the target Sku Name.
        /// </summary>
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets the target Resource Type.
        /// </summary>
        public ResourceType Type { get; set; }

        /// <summary>
        /// Gets or sets the target Location.
        /// </summary>
        public AzureLocation Location { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to create resource on the fly.
        /// </summary>
        public bool QueueCreateResource { get; set; }

        /// <summary>
        /// Gets or sets extended properties.
        /// </summary>
        public AllocateExtendedProperties ExtendedProperties { get; set; }
    }
}
