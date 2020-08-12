// <copyright file="IBillingEventToBillingWindowMapper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Calculates billing units per VSO SkuPlan in the current control plane region(s)
    /// and saves a BillingSummary to the environment_billing_events table.
    /// </summary>
    public interface IBillingEventToBillingWindowMapper
    {
        /// <summary>
        /// Computes a collection of BillingWindowSlices.
        /// </summary>
        /// <param name="currentEvent">The current event.</param>
        /// <param name="currentState">The current state.</param>
        /// <param name="endTimeForPeriod">The end time period for this window.</param>
        /// <returns>A collection of <see cref="BillingWindowSlice"/>.</returns>
        (IEnumerable<BillingWindowSlice> Slices, BillingWindowSlice.NextState NextState) ComputeNextHourBoundWindowSlices(
            BillingEvent currentEvent,
            BillingWindowSlice.NextState currentState,
            DateTime endTimeForPeriod);
    }
}