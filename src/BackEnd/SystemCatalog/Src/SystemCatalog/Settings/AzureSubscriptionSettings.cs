// <copyright file="AzureSubscriptionSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Settings
{
    /// <summary>
    /// A settings object for an Azure subscription.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class AzureSubscriptionSettings
    {
        /// <summary>
        /// Gets or sets the Azure subscription id.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the display name for this Azure subscription (informational only).
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this Azure subscription is enabled for creating new resources.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the service principal settings for this Azure subscription.
        /// </summary>
        public ServicePrincipalSettings ServicePrincipal { get; set; }
    }
}
