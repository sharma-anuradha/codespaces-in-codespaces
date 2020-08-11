// <copyright file="BillingWindowBillingState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts
{
    /// <summary>
    /// Billing Window Billing State.
    /// </summary>
    public enum BillingWindowBillingState
    {
        /// <summary>
        /// Environment is Available and should be charged the Active
        /// amount for this BillingWindow.
        /// </summary>
        Active,

        /// <summary>
        /// Environment is Shutdown and should be charged the Inactive
        /// amount for this BillingWindow.
        /// </summary>
        Inactive,

        /// <summary>
        /// Environment is Archived and should be charged the Inactive
        /// amount for this BillingWindow.
        /// The distinction between Shutdown and Archived is made for
        /// logging purposes only.
        /// </summary>
        Archived,
    }
}
