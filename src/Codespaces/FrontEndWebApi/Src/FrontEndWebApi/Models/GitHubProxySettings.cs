// <copyright file="GitHubProxySettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    /// <summary>
    /// Represents the app settings model.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class GitHubProxySettings
    {
        /// <summary>
        /// Gets or sets the SubscriptionId.
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the resource group.
        /// </summary>
        public string ResourceGroup { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string PlanName { get; set; }

        /// <summary>
        /// Gets or sets the provider namespace.
        /// </summary>
        public string ProviderNamespace { get; set; }
    }
}
