// <copyright file="BillingEventTypes.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    public static class BillingEventTypes
    {
        /// <summary>
        /// Event that occurs when we are notified by our RP that a subscription state changed.
        /// An initial event should also be emitted when each account is created.
        /// </summary>
        /// <seealso cref="SubscriptionStates" />
        /// <seealso cref="BillingStateChange" />
        public const string SubscriptionStateChange = "subscriptionStateChange";

        /// <summary>
        /// Event that occurrs when we are notified by our RP that an account plan (SKU)
        /// changed. Events must also be emitted when each account is created and deleted.
        /// </summary>
        /// <seealso cref="BillingStateChange" />
        public const string AccountPlanChange = "accountPlanChange";

        /// <summary>
        /// Event that occurrs when a cloud environment state changes (including creation and deletion).
        /// </summary>
        /// <seealso cref="EnvironmentStates" />
        /// <seealso cref="BillingStateChange" />
        public const string EnvironmentStateChange = "environmentStateChange";

        /// <summary>
        /// Event that occurs when one billing for one period has been calculated and possibly emitted.
        /// </summary>
        /// <seealso cref="BillingSummary" />
        public const string BillingSummary = "billingSummary";
    }
}
