// <copyright file="AzureSubscriptionCatalogSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Settings
{
    /// <summary>
    /// A settings object for an Azure subscription.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class AzureSubscriptionCatalogSettings
    {
        /// <summary>
        /// Gets the default list of supported locations.
        /// </summary>
        [JsonProperty]

        public List<AzureLocation> DefaultLocations { get; } = new List<AzureLocation>();

        /// <summary>
        /// Gets the list of Azure subscriptions.
        /// </summary>
        [JsonProperty]
        public List<AzureSubscriptionSettings> AzureSubscriptions { get; } = new List<AzureSubscriptionSettings>();
    }
}
