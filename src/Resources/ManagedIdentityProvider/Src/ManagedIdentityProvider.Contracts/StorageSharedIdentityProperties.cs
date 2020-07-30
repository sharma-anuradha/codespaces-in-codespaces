// <copyright file="StorageSharedIdentityProperties.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ManagedIdentityProvider
{
    /// <summary>
    /// Properties associated with the Shared Identity Response.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class StorageSharedIdentityProperties
    {
        /// <summary>
        /// Gets or sets the type of the identity, such as "SystemAssigned".
        /// </summary>
        public string IdentityType { get; set; }

        /// <summary>
        /// Gets or sets the client ID associated with the identity.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the principal ID associated with the identity.
        /// </summary>
        public string PrincipalId { get; set; }

        /// <summary>
        /// Gets or sets the tenant ID associated with the identity.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the identity URL, used to retrieve the credentials.
        /// </summary>
        public string IdentityUrl { get; set; }

        /// <summary>
        /// Gets or sets the certificate data (client secret).
        /// </summary>
        public string CertificateData { get; set; }

        /// <summary>
        /// Gets or sets the date after which to renew the credentials.
        /// </summary>
        [JsonProperty("certRenewAfter")]
        public string CertificateRenewAfter { get; set; }
    }
}
