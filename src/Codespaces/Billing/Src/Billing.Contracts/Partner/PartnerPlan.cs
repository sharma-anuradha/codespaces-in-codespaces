// <copyright file="PartnerPlan.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Contract for submission.
    /// </summary>
    public class PartnerPlan
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PartnerPlan"/> class.
        /// </summary>
        /// <param name="plan">The plan event being pushed to a partner.</param>
        public PartnerPlan(VsoPlanInfo plan)
        {
            Subscription = plan.Subscription;
            ResourceGroup = plan.ResourceGroup;
            Name = plan.Name;
            Location = plan.Location;
        }

        /// <summary>
        /// Gets or sets the ID of the subscription that contains the plan resource.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "subscription")]
        public string Subscription { get; set; }

        /// <summary>
        /// Gets or sets the name (not full path) of the resource group that contains the plan resource.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "resourceGroup")]
        public string ResourceGroup { get; set; }

        /// <summary>
        /// Gets or sets the name (not full path) of the plan resource.
        /// </summary>
        /// <remarks>
        /// The full resource path can be obtained via the <see cref="AccountExtensions.GetResourcePath()" /> extension method.
        /// </remarks>
        [JsonProperty(Required = Required.Always, PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the geo-location (region) that the plan resource is in.
        /// </summary>
        /// <remarks>
        /// All environments associated with an plan must be in the same region as the plan.
        /// <para/>
        /// At least initially there will be a separate database per region, so all entities in
        /// the same database will have the location value. But this property can allow for
        /// multiple regions sharing the same database if that is ever preferable.
        /// </remarks>
        [JsonProperty(Required = Required.Always, PropertyName = "location")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AzureLocation Location { get; set; }
    }
}
