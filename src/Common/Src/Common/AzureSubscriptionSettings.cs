// <copyright file="AzureSubscriptionSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// A settings object for an Azure subscription.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class AzureSubscriptionSettings
    {
        /// <summary>
        /// Gets or sets subscription name (optional when inferred from dictionary name).
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string SubscriptionName { get; set; }

        /// <summary>
        /// Gets or sets the Azure subscription id.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this Azure subscription is enabled for creating new resources.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the list of supported azure locations.
        /// </summary>
        [JsonProperty(Required = Required.Always, ItemConverterType = typeof(StringEnumConverter))]
        public List<AzureLocation> Locations { get; set; } = new List<AzureLocation>();

        /// <summary>
        /// Gets or sets the service principal settings for this Azure subscription.
        /// If this is null, the catalog implementation uses a default,
        /// which in our case is the application service principal.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public ServicePrincipalSettings ServicePrincipal { get; set; }

        /// <summary>
        /// Gets or sets the quotas for this subscriptions -- overrides the default quotas.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public AzureSubscriptionQuotaSettings Quotas { get; set; }
    }
}
