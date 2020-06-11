// <copyright file="RPSubscriptionProperties.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Subscriptions
{
    /// <summary>
    /// JSON body for subscription notification properties.
    /// </summary>
    public class RPSubscriptionProperties
    {
        /// <summary>
        /// Gets or sets the tenant Id.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "tenantId")]
        public Guid TenantId { get; set; }

        /// <summary>
        /// Gets or sets the location placement Id.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "locationPlacementId")]
        public string LocationPlacementId { get; set; }

        /// <summary>
        /// Gets or sets the quote Id.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "quotaId")]
        public string QuotaId { get; set; }

        /// <summary>
        /// Gets or sets the quote Id.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "accountOwner")]
        public SubscriptionAccountOwner AccountOwner { get; set; }

        /// <summary>
        /// Gets or sets the list of registered features.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "registeredFeatures")]
        public List<KeyValuePair<string, string>> RegisteredFeatures { get; set; }

        /// <summary>
        /// Gets or sets the list of managed by tenants.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "managedByTenants")]
        public List<KeyValuePair<string, string>> ManagedByTenants { get; set; }

        /// <summary>
        /// Gets or sets the additional properties.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "additionalProperties")]
        public SubscriptionAdditionalProperties AdditionalProperties { get; set; }
    }
}
