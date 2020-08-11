// <copyright file="BillingMeterSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.Azure.Documents;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts
{
    /// <summary>
    /// Contract representing billing meter definitions from appSettings.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class BillingMeterSettings
    {
        /// <summary>
        /// Gets or sets legacy meter values organized by Azure location.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public Dictionary<AzureLocation, string> MetersByLocation { get; set; } = new Dictionary<AzureLocation, string>();

        /// <summary>
        /// Gets or sets meter values organized by Resource type, Azure location, and SKU.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public ResourceBillingMeters MetersByResource { get; set; } = new ResourceBillingMeters();
    }
}
