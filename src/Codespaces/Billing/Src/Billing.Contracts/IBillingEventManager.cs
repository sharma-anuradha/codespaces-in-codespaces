﻿// <copyright file="IBillingEventManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// The billing event manager.
    /// </summary>
    public interface IBillingEventManager
    {
        /// <summary>
        /// Creates a new billing event entity in the repository, stamped with the current time.
        /// </summary>
        /// <param name="plan">Required plan that the event is associated with.</param>
        /// <param name="environment">Optional environment that the event is associated with.</param>
        /// <param name="eventType">Required event type; one of the constants from
        /// <see cref="BillingEventTypes"/>.</param>
        /// <param name="args">Required event args; the type of args must match the event type.</param>
        /// <param name="logger">Optional logger.</param>
        /// <returns>The created event entity, including unique ID and timestamp.</returns>
        Task<BillingEvent> CreateEventAsync(
            VsoPlanInfo plan,
            EnvironmentBillingInfo environment,
            string eventType,
            object args,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Gets all billing events for a specified plan within a time range.
        /// </summary>
        /// <param name="plan">Required plan to filter billing events.</param>
        /// <param name="start">Required start time (UTC). Events before this time are ignored.</param>
        /// <param name="end">Optional end time (UTC), or null to look at all events after the start time.</param>
        /// <param name="eventTypes">Optional list of one or more event types to include, or null to include
        /// all event types.</param>
        /// <param name="logger">Optional logger.</param>
        /// <returns>List of billing events matching the parameters.</returns>
        Task<IEnumerable<BillingEvent>> GetPlanEventsAsync(
            VsoPlanInfo plan,
            DateTime start,
            DateTime? end,
            ICollection<string> eventTypes,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Gets plan events asynchronously.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The billing events.</returns>
        Task<IEnumerable<BillingEvent>> GetPlanEventsAsync(
            Expression<Func<BillingEvent, bool>> filter,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Update event asynchronously.
        /// </summary>
        /// <param name="billingEvent">The billing event.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The not copy and pasted billing event.</returns>
        Task<BillingEvent> UpdateEventAsync(
            BillingEvent billingEvent,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the current override state for the given time.
        /// </summary>
        /// <param name="start">The time representing when the current state starts.</param>
        /// <param name="subscriptionId">The subscription id being sought.</param>
        /// <param name="plan"> the current SkuPlan.</param>
        /// <param name="sku">the current SKU.</param>
        /// <param name="logger">Logging service.</param>
        /// <returns>A <see cref="Task{BillingOverride}"/> representing the result of the asynchronous operation.</returns>
        Task<BillingOverride> GetOverrideStateForTimeAsync(
            DateTime start,
            string subscriptionId,
            VsoPlanInfo plan,
            Sku sku,
            IDiagnosticsLogger logger);
    }
}
