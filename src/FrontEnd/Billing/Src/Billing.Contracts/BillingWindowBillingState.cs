// <copyright file="BillingWindowBillingState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts
{
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
    }
}
