// <copyright file="ResourceBillingMeters.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts
{
    /// <summary>
    /// Class represeting all meters for Compute and Storage resources.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ResourceBillingMeters
    {
        /// <summary>
        /// Gets or sets Compute meters list.
        /// </summary>
        public List<BillingMeter> Compute { get; set; }

        /// <summary>
        /// Gets or sets Storage meters list.
        /// </summary>
        public List<BillingMeter> Storage { get; set; }
    }
}
