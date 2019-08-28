// <copyright file="AllocateResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class AllocateResult
    {
        /// <summary>
        /// The resource broker resource id.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string SkuName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ResourceType Type { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public AzureResourceInfo AzureResourceInfo { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime Created { get; set; }
    }
}
