// <copyright file="VsoPlan.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans
{
    /// <summary>
    /// Database entity that represents a plan.
    /// </summary>
    public class VsoPlan : TaggedEntity
    {
        /// <summary>
        /// Gets or sets the plan info.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "plan")]
        public VsoPlanInfo Plan { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who created the plan.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "userId")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets ID of the tenant that the plan is in.
        /// </summary>
        /// <remarks>
        /// The tenant ID can be used to direct the user to login to the correct tenant
        /// for access the plan.
        /// </remarks>
        [JsonProperty(Required = Required.Default, PropertyName = "tenant")]
        public string Tenant { get; set; }

        /// <summary>
        /// Gets or sets the billing plan selected for this plan. This corresponds to the "SKU"
        /// property on the plan resource.
        /// </summary>
        /// <remarks>
        /// Changes to this property should also be recorded as
        /// <see cref="BillingEventTypes.SkuPlanChange"/> events in the billing events collection.
        /// </remarks>
        [JsonProperty(Required = Required.Default, PropertyName = "sku")]
        public Sku SkuPlan { get; set; }

        /// <summary>
        /// Gets or sets the current state of the subscription, which may impact what operations and billing are allowed.
        /// </summary>
        /// <seealso cref="SubscriptionStates" />
        /// <remarks>
        /// Changes to this property should also be recorded as
        /// <see cref="BillingEventTypes.SubscriptionStateChange"/> events in the billing events collection.
        /// <para/>
        /// While there may be multiple plans in a subscription so subscription state is not really
        /// per-plan, it is tracked separately with each plan because billing is plan-centric.
        /// </remarks>
        [JsonProperty(Required = Required.Default, PropertyName = "subscriptionState")]
        public string SubscriptionState { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a plan is deleted, we can still bill/query for its records.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "isDeleted")]
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the date and time when the plan was deleted.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "deletedDate")]
        public DateTime? DeletedDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a final bill has been submitted for the plan.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "isFinalBillSubmitted")]
        public bool IsFinalBillSubmitted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the date and time when the final bill was submitted for the plan.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "finalBillSubmittedDate")]
        public DateTime? FinalBillSubmittedDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the plan's environments have been soft deleted.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "areEnvironmentsDeleted")]
        public bool? AreEnvironmentsDeleted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the plan's secret stores have been deleted.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "areSecretStoresDeleted")]
        public bool? AreSecretStoresDeleted { get; set; }

        /// <summary>
        /// Gets or sets the default properties for this plan.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "properties")]
        public VsoPlanProperties Properties { get; set; }

        /// <summary>
        /// Gets or sets the managed identity properties for this plan.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "managedIdentity")]
        public VsoPlanIdentity ManagedIdentity { get; set; }

        /// <summary>
        /// Gets or sets the external partner.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "partner")]
        public Partner? Partner { get; set; }
    }
}
