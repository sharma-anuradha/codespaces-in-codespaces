// <copyright file="BillingEventManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Provides higher-level APIs for creating and querying billing events.
    /// </summary>
    public class BillingEventManager : IBillingEventManager
    {
        private readonly IBillingEventRepository billingEventRepository;
        private readonly IBillingOverrideRepository billingOverrideRepository;
        private IEnumerable<BillingOverride> billingOverrideCache;
        private DateTime billingOverrideCacheTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingEventManager"/> class.
        /// </summary>
        /// <param name="billingEventRepository">Event repository.</param>
        /// <param name="billingOverrideRepository">the billing override repository.</param>
        public BillingEventManager(
            IBillingEventRepository billingEventRepository,
            IBillingOverrideRepository billingOverrideRepository)
        {
            Requires.NotNull(billingEventRepository, nameof(billingEventRepository));
            Requires.NotNull(billingOverrideRepository, nameof(billingOverrideRepository));

            this.billingOverrideRepository = billingOverrideRepository;
            this.billingEventRepository = billingEventRepository;
        }

        /// <inheritdoc/>
        public async Task<BillingEvent> CreateEventAsync(
            VsoPlanInfo plan,
            EnvironmentBillingInfo environment,
            string eventType,
            object args,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(plan, nameof(plan));
            Requires.NotNullOrEmpty(eventType, nameof(eventType));

            var duration = logger.StartDuration();
            try
            {
                var billingEvent = new BillingEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    Time = DateTime.UtcNow,
                    Plan = plan,
                    Environment = environment,
                    Type = eventType,
                    Args = args,
                };
                billingEvent = await billingEventRepository.CreateAsync(billingEvent, logger);

                logger.AddDuration(duration)
                    .AddVsoPlan(plan)
                    .LogInfo(GetType().FormatLogMessage(nameof(CreateEventAsync)));
                return billingEvent;
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .AddVsoPlan(plan)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(CreateEventAsync)), ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Updates a new billing event entity in the repository.
        /// </summary>
        /// <param name="billingEvent"> the event being updated.</param>
        /// <param name="logger">Optional logger.</param>
        /// <returns>The created event entity, including unique ID and timestamp.</returns>
        public async Task<BillingEvent> UpdateEventAsync(
            BillingEvent billingEvent,
            IDiagnosticsLogger logger)
        {
            var duration = logger.StartDuration();
            try
            {
                billingEvent = await billingEventRepository.UpdateAsync(billingEvent, logger);

                logger.AddDuration(duration)
                    .LogInfo(GetType().FormatLogMessage(nameof(UpdateEventAsync)));
                return billingEvent;
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(UpdateEventAsync)), ex.Message);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<BillingEvent>> GetPlanEventsAsync(
            VsoPlanInfo plan,
            DateTime start,
            DateTime? end,
            ICollection<string> eventTypes,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(plan, nameof(plan));
            Requires.Argument(start.Kind == DateTimeKind.Utc, nameof(start), "DateTime values must be UTC.");
            Requires.Argument(
                end == null || end.Value.Kind == DateTimeKind.Utc, nameof(end), "DateTime values must be UTC.");

            var duration = logger.StartDuration();
            try
            {
                Expression<Func<BillingEvent, bool>> where;
                if (eventTypes == null)
                {
                    // Optimize common queries with no event types or end date.
                    if (end == null)
                    {
                        where = x => x.Plan.Subscription == plan.Subscription && x.Plan.ResourceGroup == plan.ResourceGroup && x.Plan.Name == plan.Name && x.Plan.Location == plan.Location &&
                            start <= x.Time;
                    }
                    else
                    {
                        where = x => x.Plan.Subscription == plan.Subscription && x.Plan.ResourceGroup == plan.ResourceGroup && x.Plan.Name == plan.Name && x.Plan.Location == plan.Location &&
                            start <= x.Time && x.Time <= end.Value;
                    }
                }
                else if (eventTypes.Count == 1)
                {
                    var eventType = eventTypes.Single();
                    if (end == null)
                    {
                        where = x => x.Plan.Subscription == plan.Subscription && x.Plan.ResourceGroup == plan.ResourceGroup && x.Plan.Name == plan.Name && x.Plan.Location == plan.Location &&
                            start <= x.Time && x.Type == eventType;
                    }
                    else
                    {
                        where = x => x.Plan.Subscription == plan.Subscription && x.Plan.ResourceGroup == plan.ResourceGroup && x.Plan.Name == plan.Name && x.Plan.Location == plan.Location &&
                            start <= x.Time && x.Time <= end.Value && x.Type == eventType;
                    }
                }
                else
                {
                    if (end == null)
                    {
                        where = x => x.Plan.Subscription == plan.Subscription && x.Plan.ResourceGroup == plan.ResourceGroup && x.Plan.Name == plan.Name && x.Plan.Location == plan.Location &&
                            start <= x.Time && eventTypes.Contains(x.Type);
                    }
                    else
                    {
                        where = x => x.Plan.Subscription == plan.Subscription && x.Plan.ResourceGroup == plan.ResourceGroup && x.Plan.Name == plan.Name && x.Plan.Location == plan.Location &&
                            start <= x.Time && x.Time <= end.Value && eventTypes.Contains(x.Type);
                    }
                }

                // This should be a single-partition query.
                // The billing event collection is partitioned on subscription, and all queries
                // filter on plan, which includes the subscription property.
                // Ordering within the query itself creates much much more expensive query.
                var billingEvents = (await billingEventRepository.QueryAsync(
                    q => q.Where(where), logger)).OrderBy(x => x.Time);

                logger.AddDuration(duration)
                    .AddVsoPlan(plan)
                    .LogInfo(GetType().FormatLogMessage(nameof(GetPlanEventsAsync)));
                return billingEvents;
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .AddVsoPlan(plan)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(GetPlanEventsAsync)), ex.Message);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<BillingEvent>> GetPlanEventsAsync(
       Expression<Func<BillingEvent, bool>> filter,
       IDiagnosticsLogger logger)
        {
            var duration = logger.StartDuration();
            try
            {
                // This should be a single-partition query.
                // The billing event collection is partitioned on subscription, and all queries
                // filter on plan, which includes the subscription property.
                // Bug: We ordered our queries in the DocDb query itself which is a much more expensive call to docDB
                var billingEvents = (await billingEventRepository.QueryAsync(
                    q => q.Where(filter), logger)).OrderBy(x => x.Time);

                logger.AddDuration(duration)
                    .LogInfo(GetType().FormatLogMessage(nameof(GetPlanEventsAsync)));
                return billingEvents;
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)

                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(GetPlanEventsAsync)), ex.Message);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<BillingOverride> GetOverrideStateForTimeAsync(DateTime start, string subscriptionID, VsoPlanInfo plan, Sku sku, IDiagnosticsLogger logger)
        {
            Requires.Argument(start.Kind == DateTimeKind.Utc, nameof(start), "DateTime values must be UTC.");

            await CacheBillingOverrides(start, logger);
            bool TimeRangeCondition(BillingOverride x) => start >= x.StartTime && start < x.EndTime;

            var candidates = GetApplicableBillingOverrides(subscriptionID, plan, sku, TimeRangeCondition);

            return candidates.OrderBy(x => x.Priority).FirstOrDefault();
        }

        private IEnumerable<BillingOverride> GetApplicableBillingOverrides(string subscriptionID, VsoPlanInfo plan, Sku sku, Func<BillingOverride, bool> timeRangeCondition)
        {
            var candidates = new List<BillingOverride>();
            Func<BillingOverride, bool> where;

            // The algorithm is to get overrides for:
            // - All the subscrptionIDs that match
            // - All the SKUs that match
            // - All the Accounts that match
            // - All global overrides
            // Union all of these together.
            if (!string.IsNullOrEmpty(subscriptionID))
            {
                // When there's a subscription ID
                where = x => timeRangeCondition(x)
                            && subscriptionID.Equals(x.Subscription, StringComparison.OrdinalIgnoreCase)
                            && x.Plan == null
                            && x.Sku == null;
                candidates.AddRange(billingOverrideCache.Where(where));
                if (plan != null)
                {
                    where = x => timeRangeCondition(x)
                                && subscriptionID.Equals(x.Subscription, StringComparison.OrdinalIgnoreCase)
                                && x.Plan == plan
                                && x.Sku == null;
                    candidates.AddRange(billingOverrideCache.Where(where));

                    // All three have been specified on the override
                    if (sku != null)
                    {
                        where = x => timeRangeCondition(x)
                                    && subscriptionID.Equals(x.Subscription, StringComparison.OrdinalIgnoreCase)
                                    && x.Plan == plan
                                    && (x.Sku != null && sku.Name.Equals(x.Sku.Name, StringComparison.OrdinalIgnoreCase));
                        candidates.AddRange(billingOverrideCache.Where(where));
                    }
                }
            }

            if (plan != null)
            {
                where = x => timeRangeCondition(x)
                                && x.Subscription == null
                                && x.Plan == plan
                                && x.Sku == null;
                candidates.AddRange(billingOverrideCache.Where(where));

                // SkuPlan and Sku have been specified on the override
                if (sku != null)
                {
                    where = x => timeRangeCondition(x)
                                && x.Subscription == null
                                && x.Plan == plan
                                && (x.Sku != null && sku.Name.Equals(x.Sku.Name, StringComparison.OrdinalIgnoreCase));
                    candidates.AddRange(billingOverrideCache.Where(where));
                }
            }

            if (sku != null)
            {
                where = x => timeRangeCondition(x)
                            && x.Subscription == null
                            && x.Plan == null
                            && (x.Sku != null && sku.Name.Equals(x.Sku.Name, StringComparison.OrdinalIgnoreCase));
                candidates.AddRange(billingOverrideCache.Where(where));
            }

            if (!string.IsNullOrEmpty(subscriptionID) && sku != null)
            {
                where = x => timeRangeCondition(x)
                        && subscriptionID.Equals(x.Subscription, StringComparison.OrdinalIgnoreCase)
                        && x.Plan == null
                        && (x.Sku != null && sku.Name.Equals(x.Sku.Name, StringComparison.OrdinalIgnoreCase));
                candidates.AddRange(billingOverrideCache.Where(where));
            }

            // Add in the gloal overrides
            where = x => timeRangeCondition(x)
                    && x.Subscription == null
                    && x.Plan == null
                    && x.Sku == null;
            candidates.AddRange(billingOverrideCache.Where(where));
            return candidates;
        }

        private async Task CacheBillingOverrides(DateTime startTime, IDiagnosticsLogger logger)
        {
            try
            {
                // Note: We're getting this whole collection right now but this hinges on the fact that it is small and likely only used in emergency casese. If not, we would potentially want
                // to apply filters at a different point in time.
                if (billingOverrideCache is null || startTime > billingOverrideCacheTime)
                {
                    var duration = logger.StartDuration();
                    var billingOverride = await billingOverrideRepository.QueryAsync(
                    q => q, logger);
                    billingOverrideCache = billingOverride.ToList();
                    billingOverrideCacheTime = DateTime.Now;
                    logger.AddDuration(duration)
                        .LogInfo(GetType().FormatLogMessage(nameof(CacheBillingOverrides)));
                }
            }
            catch (Exception ex)
            {
                logger.LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(CacheBillingOverrides)), ex.Message);
                throw;
            }
        }
    }
}
