// <copyright file="RPRegisteredSubscriptionsRequest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Subscriptions
{
    /// <summary>
    /// Request contract for the RPaaS RegisteredSubscriptions Endpoint.
    /// </summary>
    public class RPRegisteredSubscriptionsRequest
    {
        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "subscriptionState")]
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the tenant Id.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "tenantId")]
        public Guid TenantId { get; set; }

        /// <summary>
        /// Gets or sets the resource provider namespace.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "resourceProviderNamespace")]
        public string ResourceProviderNamespace { get; set; }

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
        /// Gets or sets the list of registered features.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "registeredFeatures")]
        public List<KeyValuePair<string, string>> RegisteredFeatures { get; set; }

        /// <summary>
        /// Gets or sets the subscription spending limit.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "subscriptionSpendingLimit")]
        public string SubscriptionSpendingLimit { get; set; }

        /// <summary>
        /// Gets or sets the subscription account owner.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "subscriptionAccountOwner")]
        public string SubscriptionAccountOwner { get; set; }

        /// <summary>
        /// Gets or sets the list of managed by tenants.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "managedByTenants")]
        public List<KeyValuePair<string, string>> ManagedByTenants { get; set; }
    }
}
