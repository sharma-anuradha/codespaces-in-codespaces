// <copyright file="CredentialResponse.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ManagedIdentityProvider
{
    /// <summary>
    /// A system assigned managed identity.
    /// </summary>
    public class CredentialResponse
    {
        /// <summary>
        /// Gets or sets the AAD client id for the system assigned identity.
        /// </summary>
        [JsonProperty("client_id", NullValueHandling = NullValueHandling.Ignore)]
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the base64 encoded private key X509 certificate for the system assigned identity.
        /// </summary>
        [JsonProperty("client_secret", NullValueHandling = NullValueHandling.Ignore)]
        public string ClientSecret { get; set; }

        /// <summary>
        /// Gets or sets a refreshed version of the URL used to retrieve credentials for the system assigned identity.
        /// </summary>
        [JsonProperty("client_secret_url", NullValueHandling = NullValueHandling.Ignore)]
        public string ClientSecretUrl { get; set; }

        /// <summary>
        /// Gets or sets an internal identifier for the resource in managed identity RP. Used by CRP. Can be ignored by other RPs.
        /// </summary>
        [JsonProperty("internal_id", NullValueHandling = NullValueHandling.Ignore)]
        public string InternalId { get; set; }

        /// <summary>
        /// Gets or sets the AAD object id for the system assigned identity.
        /// </summary>
        [JsonProperty("object_id", NullValueHandling = NullValueHandling.Ignore)]
        public string ObjectId { get; set; }

        /// <summary>
        /// Gets or sets the time at which the system assigned credential becomes valid for retireving AAD tokens in the format 2017-03-01T14:11:00Z.
        /// </summary>
        [JsonProperty("not_before", NullValueHandling = NullValueHandling.Ignore)]
        public string NotBefore { get; set; }

        /// <summary>
        /// Gets or sets the time at which the system assigned credential becomes invalid for retireving AAD tokens in the format 2017-03-01T14:11:00Z.
        /// </summary>
        [JsonProperty("not_after", NullValueHandling = NullValueHandling.Ignore)]
        public string NotAfter { get; set; }

        /// <summary>
        /// Gets or sets the time after which a call to the system assigned client_secret_url will return a new credential in the format 2017-03-01T14:11:00Z.
        /// </summary>
        [JsonProperty("renew_after", NullValueHandling = NullValueHandling.Ignore)]
        public string RenewAfter { get; set; }

        /// <summary>
        /// Gets or sets the time after which the system assigned client_secret cannot be used to call client_secret_url for a refreshed credential in the format 2017-03-01T14:11:00Z.
        /// </summary>
        [JsonProperty("cannot_renew_after", NullValueHandling = NullValueHandling.Ignore)]
        public string CannotRenewAfter { get; set; }

        /// <summary>
        /// Gets or sets the AAD tenant id for the system assigned identity.
        /// </summary>
        [JsonProperty("tenant_id", NullValueHandling = NullValueHandling.Ignore)]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the AAD authentication endpoint for the identity system assigned identity. You can make token request toward this authentication endpoint.
        /// </summary>
        [JsonProperty("authentication_endpoint", NullValueHandling = NullValueHandling.Ignore)]
        public string AuthenticationEndpoint { get; set; }

        /// <summary>
        /// Returns the string presentation of the object.
        /// </summary>
        /// <returns>String presentation of the object.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class CredentialResponse {\n");
            sb.Append("  ClientId: ").Append(ClientId).Append("\n");
            sb.Append("  ClientSecret: ").Append(ClientSecret).Append("\n");
            sb.Append("  ClientSecretUrl: ").Append(ClientSecretUrl).Append("\n");
            sb.Append("  InternalId: ").Append(InternalId).Append("\n");
            sb.Append("  ObjectId: ").Append(ObjectId).Append("\n");
            sb.Append("  NotBefore: ").Append(NotBefore).Append("\n");
            sb.Append("  NotAfter: ").Append(NotAfter).Append("\n");
            sb.Append("  RenewAfter: ").Append(RenewAfter).Append("\n");
            sb.Append("  CannotRenewAfter: ").Append(CannotRenewAfter).Append("\n");
            sb.Append("  TenantId: ").Append(TenantId).Append("\n");
            sb.Append("  AuthenticationEndpoint: ").Append(AuthenticationEndpoint).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }
    }
}
