// <copyright file="FirstPartyAppSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// A settings object for an Azure Service Principal.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class FirstPartyAppSettings
    {
        /// <summary>
        /// Gets or sets the service principal tenant id.
        /// </summary>
        public string AuthorityTenantId { get; set; }

        /// <summary>
        /// Gets or sets the authority url.
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// Gets or sets the Scope.
        /// </summary>
        public string Scope { get; set; }
    }
}
