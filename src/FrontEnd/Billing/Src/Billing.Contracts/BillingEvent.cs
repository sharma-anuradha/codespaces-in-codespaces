// <copyright file="BillingEvent.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Database entity that represents an event that is important to track either
    /// directly for billing or for billing-related monitoring & consistency checking.
    /// </summary>
    public class BillingEvent : TaggedEntity
    {
        /// <summary>
        /// Gets or sets UTC time when the event occurred.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "time")]
        public DateTime Time { get; set; }

        /// <summary>
        /// Gets or sets information about the plan that the event relates to.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "plan")]
        public VsoPlanInfo Plan { get; set; }

        /// <summary>
        /// Gets or sets optional environment info. Required for some event types, but may be omitted
        /// if the event type is not associated with a specific environment.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "environment")]
        public EnvironmentBillingInfo Environment { get; set; }

        /// <summary>
        /// Gets or sets one of the <see cref="BillingEventTypes" /> constants.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the type of object depends on the <see cref="EventType" />.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "args")]
        [JsonConverter(typeof(BillingEventArgsConverter))]
        public object Args { get; set; }
    }
}
