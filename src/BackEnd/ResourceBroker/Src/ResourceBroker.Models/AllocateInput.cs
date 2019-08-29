﻿// <copyright file="AllocateInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

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
        public string Location { get; set; }
    }
}
