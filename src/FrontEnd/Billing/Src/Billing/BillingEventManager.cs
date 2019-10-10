// <copyright file="BillingEventManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Accounts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Provides higher-level APIs for creating and querying billing events.
    /// </summary>
    public class BillingEventManager : IBillingEventManager
    {
        private readonly IEnumerable<string> guidChars = new List<string> { "a", "b", "c", "d", "e", "f", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }.Shuffle();
        private readonly IBillingEventRepository billingEventRepository;
       
        /// <summary>
        /// Initializes a new instance of the <see cref="BillingEventManager"/> class.
        /// </summary>
        /// <param name="billingEventRepository">Event repository.</param>
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
            VsoAccountInfo account,
            EnvironmentBillingInfo environment,
            string eventType,
            object args,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(account, nameof(account));
            Requires.NotNullOrEmpty(eventType, nameof(eventType));

            var duration = logger.StartDuration();
            try
            {
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

                logger.AddDuration(duration)
                    .AddAccount(account)
                    .LogInfo(GetType().FormatLogMessage(nameof(CreateEventAsync)));
                return billingEvent;
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .AddAccount(account)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(CreateEventAsync)), ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Updates a new billing event entity in the repository
        /// </summary>
        /// <param name="billingEvent"> the event being updated</param>
        /// <param name="logger">Optional logger.</param>
        /// <returns>The created event entity, including unique ID and timestamp.</returns>
        public async Task<BillingEvent> UpdateEventAsync(
            BillingEvent billingEvent,
            IDiagnosticsLogger logger)
        {
            var duration = logger.StartDuration();
            try
            {
                billingEvent = await this.billingEventRepository.UpdateAsync(billingEvent, logger);

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

        /// <summary>
        /// Gets all accounts for which there are any billing events in a specified time range.
        /// </summary>
        /// <param name="start">Required start time (UTC). Events before this time are ignored.</param>
        /// <param name="end">Optional end time (UTC), or null to look at all events after the start time.</param>
        /// <param name="logger">Optional logger.</param>
        /// <param name="locations">Azure regions to search.</param>
        /// <returns>List of distinct accounts of all billing events within the specified time.</returns>
        public async Task<IEnumerable<VsoAccountInfo>> GetAccountsAsync(
            DateTime start,
            DateTime? end,
            IDiagnosticsLogger logger,
            ICollection<AzureLocation> locations)
        {
            Requires.Argument(start.Kind == DateTimeKind.Utc, nameof(start), "DateTime values must be UTC.");
            Requires.Argument(
                end == null || end.Value.Kind == DateTimeKind.Utc, nameof(end), "DateTime values must be UTC.");
            Requires.NotNull(locations, nameof(locations));
            Requires.Argument(locations.Any(), nameof(locations), "locations collection must not me empty.");

            var duration = logger.StartDuration();
            try
            {
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

                // TODO: pagedcallback 200ms delay
                var accountsPreFiltered = await this.billingEventRepository.QueryAsync(
                    q => q.Where(where).Select(bev => bev.Account).Distinct(),
                    logger);
                var accounts = accountsPreFiltered.Where(a => locations.Contains(a.Location));
                logger.AddDuration(duration)
                    .LogInfo(GetType().FormatLogMessage(nameof(GetAccountsAsync)));
                return accounts;
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(GetAccountsAsync)), ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Returns all accounts for which there are any billing events in a specified time range
        /// and has a subscriptionId that begins with the shard value.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="logger"></param>
        /// <param name="locations"></param>
        /// <param name="shard"></param>
        /// <returns></returns>
        public async Task<IEnumerable<VsoAccountInfo>> GetAccountsByShardAsync(
            DateTime start,
            DateTime? end,
            IDiagnosticsLogger logger,
            ICollection<AzureLocation> locations,
            string shard)
        {
            Requires.Argument(start.Kind == DateTimeKind.Utc, nameof(start), "DateTime values must be UTC.");
            Requires.Argument(
                end == null || end.Value.Kind == DateTimeKind.Utc, nameof(end), "DateTime values must be UTC.");
            Requires.NotNull(locations, nameof(locations));
            Requires.Argument(locations.Any(), nameof(locations), "locations collection must not me empty.");
            Requires.NotNullOrEmpty(shard, nameof(shard));

            var duration = logger.StartDuration();
            try
            {
                Expression<Func<BillingEvent, bool>> where;
                if (end == null)
                {
                    // Optimize common queries with no end date.
                    where = bev => start <= bev.Time && bev.Account.Subscription.StartsWith(shard);
                }
                else
                {
                    where = bev => start <= bev.Time && bev.Time < end.Value && bev.Account.Subscription.StartsWith(shard);
                }

                // TODO: pagedcallback 200ms delay
                // TODO: move locations.Contains() to Expression definition
                var accounts = (await this.billingEventRepository.QueryAsync(
                    q => q.Where(where).Select(bev => bev.Account).Distinct(),
                    logger)).Where(t => locations.Contains(t.Location));

                logger.AddDuration(duration)
                    .LogInfo(GetType().FormatLogMessage(nameof(GetAccountsByShardAsync)));
                return accounts;
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(GetAccountsByShardAsync)), ex.Message);
                throw;
            }
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
        /// <param name="filter"> optional called provided filter</param>
        /// <returns>List of billing events matching the parameters.</returns>
        public async Task<IEnumerable<BillingEvent>> GetAccountEventsAsync(
            VsoAccountInfo account,
            DateTime start,
            DateTime? end,
            ICollection<string> eventTypes,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(account, nameof(account));
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

                logger.AddDuration(duration)
                    .AddAccount(account)
                    .LogInfo(GetType().FormatLogMessage(nameof(GetAccountEventsAsync)));
                return billingEvents;
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .AddAccount(account)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(GetAccountEventsAsync)), ex.Message);
                throw;
            }
        }

        public async Task<IEnumerable<BillingEvent>> GetAccountEventsAsync(
       Expression<Func<BillingEvent, bool>> filter,
       IDiagnosticsLogger logger)
        {

            var duration = logger.StartDuration();
            try
            {
                // This should be a single-partition query.
                // The billing event collection is partitioned on subscription, and all queries
                // filter on account, which includes the subscription property.
                var billingEvents = await this.billingEventRepository.QueryAsync(
                    q => q.Where(filter).OrderBy(bev => bev.Time), logger);

                logger.AddDuration(duration)
                    .LogInfo(GetType().FormatLogMessage(nameof(GetAccountEventsAsync)));
                return billingEvents;
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(GetAccountEventsAsync)), ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Returns the Account sharding mechanism. We have currently sharing by SubscriptionId so the returned list
        /// includes all availabe chars in a 16 bit GUID.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetShards()
        {
            // Represents all the available chars in a 16 bit GUID.
            return guidChars;
        }
    }
}
