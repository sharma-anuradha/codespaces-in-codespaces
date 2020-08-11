// <copyright file="BillingMeter.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts
{
    /// <summary>
    /// Class representing a billing meter.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class BillingMeter
    {
        /// <summary>
        /// Gets or sets the meter ID.
        /// </summary>
        public string MeterId { get; set; }

        /// <summary>
        /// Gets or sets the CloudEnvironment Sku.
        /// </summary>
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets the date in which the meter becomes active.
        /// </summary>
        public DateTime EnabledOnDate { get; set; }

        /// <summary>
        /// Gets or sets the Azure region.
        /// </summary>
        public AzureLocation Region { get; set; }
    }
}
