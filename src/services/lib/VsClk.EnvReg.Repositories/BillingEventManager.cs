﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using Microsoft.VsCloudKernel.Services.EnvReg.Repositories;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsCloudKernel.Services.VsClk.EnvReg.Repositories
{
    /// <summary>
    /// Provides higher-level APIs for creating and querying billing events.
    /// </summary>
    public class BillingEventManager : IBillingEventManager
    {
        private readonly IBillingEventRepository billingEventRepository;

        public BillingEventManager(
            IBillingEventRepository billingEventRepository)
        {
            Requires.NotNull(billingEventRepository, nameof(billingEventRepository));

            this.billingEventRepository = billingEventRepository;
        }

        /// <summary>
        /// Creates a new billing event entity in the repository, stamped with the current time.
        /// </summary>
        /// <param name="account">Required account that the event is associated with.</param>
        /// <param name="environment">Optional environment that the event is associated with.</param>
        /// <param name="eventType">Required event type; one of the constants from
        /// <see cref="BillingEventTypes"/>.</param>
        /// <param name="args">Required event args; the type of args must match the event type.</param>
        /// <param name="logger">Optional logger.</param>
        /// <returns>The created event entity, including unique ID and timestamp.</returns>
        public async Task<BillingEvent> CreateEventAsync(
            BillingAccountInfo account,
            EnvironmentInfo environment,
            string eventType,
            object args,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(account, nameof(account));
            Requires.NotNullOrEmpty(eventType, nameof(eventType));

            var billingEvent = new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Time = DateTime.UtcNow,
                Account = account,
                Environment = environment,
                Type = eventType,
                Args = args,
            };
            billingEvent = await this.billingEventRepository.CreateAsync(billingEvent, logger);
            return billingEvent;
        }

        /// <summary>
        /// Gets all accounts for which there are any billing events in a specified time range.
        /// </summary>
        /// <param name="start">Required start time (UTC). Events before this time are ignored.</param>
        /// <param name="end">Optional end time (UTC), or null to look at all events after the start time.</param>
        /// <param name="logger">Optional logger.</param>
        /// <returns>List of distinct accounts of all billing events within the specified time.</returns>
        public async Task<IEnumerable<BillingAccountInfo>> GetAccountsAsync(
            DateTime start,
            DateTime? end,
            IDiagnosticsLogger logger)
        {
            Requires.Argument(start.Kind == DateTimeKind.Utc, nameof(start), "DateTime values must be UTC.");
            Requires.Argument(
                end == null || end.Value.Kind == DateTimeKind.Utc, nameof(end), "DateTime values must be UTC.");

            Expression<Func<BillingEvent, bool>> where;
            if (end == null)
            {
                // Optimize common queries with no end date.
                where = bev => start <= bev.Time;
            }
            else
            {
                where = bev => start <= bev.Time && bev.Time < end.Value;
            }

            var accounts = await this.billingEventRepository.QueryAsync(
                q => q.Where(where).Select(bev => bev.Account).Distinct(),
                logger);
            return accounts;
        }

        /// <summary>
        /// Gets all billing events for a specified account within a time range.
        /// </summary>
        /// <param name="account">Required account to filter billing events.</param>
        /// <param name="start">Required start time (UTC). Events before this time are ignored.</param>
        /// <param name="end">Optional end time (UTC), or null to look at all events after the start time.</param>
        /// <param name="eventTypes">Optional list of one or more event types to include, or null to include
        /// all event types.</param>
        /// <param name="logger">Optional logger.</param>
        /// <returns>List of billing events matching the parameters.</returns>
        public async Task<IEnumerable<BillingEvent>> GetAccountEventsAsync(
            BillingAccountInfo account,
            DateTime start,
            DateTime? end,
            ICollection<string> eventTypes,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(account, nameof(account));
            Requires.Argument(start.Kind == DateTimeKind.Utc, nameof(start), "DateTime values must be UTC.");
            Requires.Argument(
                end == null || end.Value.Kind == DateTimeKind.Utc, nameof(end), "DateTime values must be UTC.");

            Expression<Func<BillingEvent, bool>> where;
            if (eventTypes == null)
            {
                // Optimize common queries with no event types or end date.
                if (end == null)
                {
                    where = bev => bev.Account == account &&
                        start <= bev.Time;
                }
                else
                {
                    where = bev => bev.Account == account &&
                        start <= bev.Time && bev.Time < end.Value;
                }
            }
            else if (eventTypes.Count == 1)
            {
                string eventType = eventTypes.Single();
                if (end == null)
                {
                    where = bev => bev.Account == account &&
                        start <= bev.Time && bev.Type == eventType;
                }
                else
                {
                    where = bev => bev.Account == account &&
                        start <= bev.Time && bev.Time < end.Value && bev.Type == eventType;
                }
            }
            else
            {
                if (end == null)
                {
                    where = bev => bev.Account == account &&
                        start <= bev.Time && eventTypes.Contains(bev.Type);
                }
                else
                {
                    where = bev => bev.Account == account &&
                        start <= bev.Time && bev.Time < end.Value && eventTypes.Contains(bev.Type);
                }
            }

            // This should be a single-partition query.
            // The billing event collection is partitioned on subscription, and all queries
            // filter on account, which includes the subscription property.
            var billingEvents = await this.billingEventRepository.QueryAsync(
                q => q.Where(where).OrderBy(bev => bev.Time), logger);

            return billingEvents;
        }
    }
}
