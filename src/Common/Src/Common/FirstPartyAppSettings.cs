// <copyright file="FirstPartyAppSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common.Identity;
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
        private const string DefaultMsiAppId = "5e40b565-3ac2-4fbc-871e-068b78151bb0";

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

        /// <summary>
        /// Gets or sets the domain name.
        /// </summary>
        public string DomainName { get; set; }

        /// <summary>
        /// Gets or sets the msi first party app id.
        /// </summary>
        public string MsiFirstPartyAppId { get; set; } = DefaultMsiAppId;

        /// <summary>
        /// Gets or sets the api first party app certificate name.
        /// </summary>
        public string ApiFirstPartyAppId { get; set; } = AuthenticationConstants.VisualStudioServicesApiAppId;
    }
}