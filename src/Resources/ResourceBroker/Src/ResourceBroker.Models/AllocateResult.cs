// <copyright file="AllocateResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models
{
    /// <summary>
    /// Input required for Allocation result.
    /// </summary>
    public class AllocateResult
    {
        /// <summary>
        /// Gets or sets the resource broker resource id.
        /// </summary>
        public Guid Id { get; set; }

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
        /// Gets or sets the Allocated Resource Info.
        /// </summary>
        public AzureResourceInfo AzureResourceInfo { get; set; }

        /// <summary>
        /// Gets or sets the time the resource was created.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether record is ready.
        /// </summary>
        public bool IsReady { get; set; }
    }
}
