// <copyright file="UserSubscription.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Runtime.Serialization;
using Microsoft.VsSaaS.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserSubscriptions
{
    /// <summary>
    /// Model for new users to subscribe to vsonline.
    /// </summary>
    [DataContract]
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class UserSubscription : TaggedEntity
    {
        /// <summary>
        /// Gets or sets the vsonline user id to correlate with profile.
        /// </summary>
        [DataMember(IsRequired = true, EmitDefaultValue = false)]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the subscription was modified.
        /// </summary>
        [DataMember(IsRequired = true, EmitDefaultValue = false)]
        public DateTime Timestamp { get; set; }
    }
}
