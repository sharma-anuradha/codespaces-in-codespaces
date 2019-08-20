// <copyright file="VsoAccount.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Accounts
{
    /// <summary>
    /// Database entity that represents an account
    /// </summary>
    public class VsoAccount : TaggedEntity
    {
        [JsonProperty(Required = Required.Always, PropertyName = "account")]
        public VsoAccountInfo Account { get; set; }

        /// <summary>
        /// The billing plan selected for this account. This corresponds to the "SKU"
        /// property on the account resource.
        /// </summary>
        /// <remarks>
        /// Changes to this property should also be recorded as
        /// <see cref="BillingEventTypes.AccountPlanChange"/> events in the billing events collection.
        /// </remarks>
        [JsonProperty(Required = Required.Default, PropertyName = "sku")]
        public Sku Plan { get; set; }

        /// <summary>
        /// Current state of the subscription, which may impact what operations and billing are allowed.
        /// </summary>
        /// <seealso cref="SubscriptionStates" />
        /// <remarks>
        /// Changes to this property should also be recorded as
        /// <see cref="BillingEventTypes.SubscriptionStateChange"/> events in the billing events collection.
        /// <para/>
        /// While there may be multiple accounts in a subscription so subscription state is not really
        /// per-account, it is tracked separately with each account because billing is account-centric.
        /// </remarks>
        [JsonProperty(Required = Required.Default, PropertyName = "subscriptionState")]
        public string SubscriptionState { get; set; }
    }
}
