// <copyright file="BillingOverrideState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    public enum BillingOverrideState
    {
        /// <summary>
        /// The OverrideState for when billing is re-enabled
        /// </summary>
        BillingEnabled,

        /// <summary>
        /// The overrideState for when billing is disabled
        /// </summary>
        BillingDisabled,
    }
}