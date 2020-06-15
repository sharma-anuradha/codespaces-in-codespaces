// <copyright file="Subscription.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions
{
    /// <summary>
    /// Database entity that represents a subscription.
    /// </summary>
    public class Subscription : TaggedEntity
    {
        /// <summary>
        /// Gets or sets the date the subscription was banned.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "dateBanned")]
        public DateTime? DateBanned { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether  the ban has been processed by the system.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "banComplete")]
        public bool BanComplete { get; set; }

        /// <summary>
        /// Gets or sets the reason the subscription was banned.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "bannedReaason")]
        public BannedReason BannedReason { get; set; }

        /// <summary>
        /// Gets or sets the user or identity who marked this subscription as banned.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "bannedByIdentity")]
        public string BannedByIdentity { get; set; }

        /// <summary>
        /// Gets or sets the subscription's QuotaId.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "quotaId")]
        public string QuotaId { get; set; }

        /// <summary>
        /// Gets or sets the subscription's state.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "state")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SubscriptionStateEnum SubscriptionState { get; set; }

        /// <summary>
        /// Gets or sets the subscription's state last update Date.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "stateUpdateDate")]
        public DateTime SubscriptionStateUpdateDate { get; set; }

        /// <summary>
        /// Gets or sets the maximum quota for this subscription. This is used as an override for defaults in the configuration.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "maxComputeQuota")]
        public IDictionary<string, int> MaximumComputeQuota { get; set; }

        /// <summary>
        /// Gets or sets the maximum quota for this subscription. This is used as an override for defaults in the Db.
        /// </summary>
        [JsonIgnore]
        public IDictionary<string, int> CurrentMaximumQuota { get; set; }

        /// <summary>
        /// Gets a value indicating whether the Subscription is banned.
        /// </summary>
        [JsonIgnore]
        public bool IsBanned => BannedReason != BannedReason.None;

        /// <summary>
        /// Gets or sets a value indicating whether the Subscription can create Environments and Plans.
        /// </summary>
        [JsonIgnore]
        public bool CanCreateEnvironmentsAndPlans { get; set; } = true;
    }
}
