// <copyright file="IUsageDetail.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Usage details.
    /// </summary>
    public interface IUsageDetail
    {
        /// <summary>
        /// Gets or sets the usage time in seconds.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "usage")]
        public double Usage { get; set; }

        /// <summary>
        /// Gets or sets the sku to which the resource is apart.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "sku")]
        public string Sku { get; set; }
    }
}
