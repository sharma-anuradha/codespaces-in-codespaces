// <copyright file="EnvironmentStateChange.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts
{
    /// <summary>
    /// Tracks environment state transitions.
    /// </summary>
    public class EnvironmentStateChange : CosmosDbEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentStateChange"/> class.
        /// </summary>
        public EnvironmentStateChange()
        {
            Id = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentStateChange"/> class.
        /// </summary>
        /// <param name="id">The ID.</param>
        public EnvironmentStateChange(Guid id)
        {
            Id = id.ToString();
        }

        /// <summary>
        /// Gets or sets the Plan ID.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "planId")]
        public string PlanId { get; set; }

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
        /// Gets or sets the environment's old state value.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "oldValue")]
        public string OldValue { get; set; }

        /// <summary>
        /// Gets or sets the environments new state value.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "newValue")]
        public string NewValue { get; set; }
    }
}
