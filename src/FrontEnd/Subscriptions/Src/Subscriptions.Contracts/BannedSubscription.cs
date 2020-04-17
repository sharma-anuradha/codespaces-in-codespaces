// <copyright file="BannedSubscription.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions
{
    /// <summary>
    /// Database entity that represents a banned subscription.
    /// </summary>
    public class BannedSubscription : TaggedEntity
    {
        /// <summary>
        /// Gets or sets the external partner.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "dateBanned")]
        public DateTime? DateBanned { get; set; }

        /// <summary>
        /// Gets or sets the external partner.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "bannedReaason")]
        public BannedReason BannedReason { get; set; }

        /// <summary>
        /// Gets or sets the user or identity who marked this subscription as banned.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "bannedByIdentity")]
        public string BannedByIdentity { get; set; }
    }
}
