// <copyright file="EnvironmentStatsGenerator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UsageAnalytics
{
    /// <summary>
    /// Helper to compute usage stats of an environment's list of billing events.
    /// </summary>
    public class EnvironmentStatsGenerator
    {
        private const string LoggerName = "environment_stats_generator";

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentStatsGenerator"/> class.
        /// </summary
        /// <param name="billingWindowMapper">An IBillingEventToBillingWindowMapper.</param>
        public EnvironmentStatsGenerator(
            IBillingEventToBillingWindowMapper billingWindowMapper)
        {
            BillingEventToBillingWindowMapper = Requires.NotNull(billingWindowMapper, nameof(billingWindowMapper));
        }

        private IBillingEventToBillingWindowMapper BillingEventToBillingWindowMapper { get; }

        /// <summary>
        /// Computes stats for an environment.
        /// </summary>
        /// <param name="billingEvents">A list of billing events.</param>
        /// <param name="endDate">The period end for the stats.</param>
        /// <param name="logger">A logger.</param>
        /// <returns>The stats for an environment.</returns>
        public Task<EnvironmentStats> GetStats(
            IEnumerable<BillingEvent> billingEvents,
            DateTime endDate,
            IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LoggerName}_get_env_stats",
                (childLogger) => Task.FromResult(GetStats(billingEvents, endDate)));
        }

        private EnvironmentStats GetStats(IEnumerable<BillingEvent> billingEvents, DateTime endDate)
        {
            EnvironmentStats stats = new EnvironmentStats();
            var initialSlice = new BillingWindowSlice.NextState
            {
                EnvironmentState = CloudEnvironmentState.None,
                Sku = null,
                TransitionTime = DateTime.UtcNow,
            };

            var allSlices = GenerateSlices(initialSlice, endDate, billingEvents);
            if (!allSlices.Any())
            {
                return stats;
            }

            stats.CreationTime = allSlices.First().StartTime;
            if (allSlices.Any(x => x.BillingState == BillingWindowBillingState.Active))
            {
                stats.TotalTimeActive = allSlices.Where(x => x.BillingState == BillingWindowBillingState.Active).Sum(x => (x.EndTime - x.StartTime).TotalHours);
            }

            if (allSlices.Any(x => x.BillingState == BillingWindowBillingState.Inactive))
            {
                stats.TotalTimeSuspend = allSlices.Where(x => x.BillingState == BillingWindowBillingState.Inactive).Sum(x => (x.EndTime - x.StartTime).TotalHours);
            }

            stats.NumberOfTimesActive = 0;
            stats.NumberOfTimeShutdown = 0;
            bool active = false;
            foreach (BillingEvent ev in billingEvents)
            {
                var currentState = ParseEnvironmentState((ev.Args as BillingStateChange).NewValue);
                switch (currentState)
                {
                    case CloudEnvironmentState.Shutdown:
                    case CloudEnvironmentState.Deleted:
                        if (active)
                        {
                            stats.NumberOfTimeShutdown++;
                            active = false;
                        }

                        break;
                    case CloudEnvironmentState.Available:
                        if (!active)
                        {
                            stats.NumberOfTimesActive++;
                            active = true;
                        }

                        break;
                    default:
                        break;
                }
            }

            var (avgTimeToShutdown, avgTimeToNextUse, maxTimeToNextUse) = ComputeUsageCycles(billingEvents);

            stats.AverageTimeToShutdown = avgTimeToShutdown;
            stats.AverageTimeToNextUse = avgTimeToNextUse;
            stats.MaxTimeToNextUse = maxTimeToNextUse;

            return stats;
        }

        private (double avgTimeToShutdown, double avgTimeToNextUse, double maxTimeToNextUse) ComputeUsageCycles(IEnumerable<BillingEvent> events)
        {
            var timesToShutdown = new List<double>();
            var timesToNextStart = new List<double>();

            var states = events.Select(ev =>
            {
                Enum.TryParse<CloudEnvironmentState>((ev.Args as BillingStateChange).NewValue, out var result);
                return (result, ev.Time);
            });

            var prevState = CloudEnvironmentState.None;
            var prevTime = DateTime.MinValue;

            foreach (var (state, time) in states)
            {
                switch (prevState)
                {
                    case CloudEnvironmentState.None:
                        if (state == CloudEnvironmentState.Available)
                        {
                            prevState = state;
                            prevTime = time;
                        }

                        break;
                    case CloudEnvironmentState.Available:
                        if (state == CloudEnvironmentState.Shutdown)
                        {
                            timesToShutdown.Add((time - prevTime).TotalHours);
                            prevState = state;
                            prevTime = time;
                        }

                        break;
                    case CloudEnvironmentState.Shutdown:
                        if (state == CloudEnvironmentState.Available)
                        {
                            timesToNextStart.Add((time - prevTime).TotalHours);
                            prevState = state;
                            prevTime = time;
                        }

                        break;
                }
            }

            var avgTimeToShutdown = timesToShutdown.DefaultIfEmpty(0).Average();
            var avgTimeToNextUse = timesToNextStart.DefaultIfEmpty(0).Average();
            var maxTimeToNextUse = timesToNextStart.DefaultIfEmpty(0).Max();

            return (avgTimeToShutdown, avgTimeToNextUse, maxTimeToNextUse);
        }

        /// <summary>
        /// Helper method to parse a cloud environment state from its string representation.
        /// </summary>
        /// <param name="state">The string representing the state being parsed.</param>
        /// <returns>The parsed state enum value.</returns>
        private CloudEnvironmentState ParseEnvironmentState(string state)
        {
            return (CloudEnvironmentState)Enum.Parse(typeof(CloudEnvironmentState), state);
        }

        private IEnumerable<BillingWindowSlice> GenerateSlices(BillingWindowSlice.NextState initialState, DateTime end, IEnumerable<BillingEvent> events)
        {
            var slices = new List<BillingWindowSlice>();
            IEnumerable<BillingWindowSlice> allSlices;
            var currState = initialState;

            // Loop through each billing event for the current environment.
            foreach (var evnt in events)
            {
                (allSlices, currState) = GenerateWindowSlices(end, currState, evnt);
                if (allSlices.Any())
                {
                    slices.AddRange(allSlices);
                }
            }

            // Get the remainder or the entire window if there were no events.
            (allSlices, _) = GenerateWindowSlices(end, currState, null);
            if (allSlices.Any())
            {
                slices.AddRange(allSlices);
            }

            return slices;
        }

        private (IEnumerable<BillingWindowSlice> Slices, BillingWindowSlice.NextState NextState) GenerateWindowSlices(DateTime end, BillingWindowSlice.NextState currState, BillingEvent evnt)
        {
            var (currSlice, nextState) = BillingEventToBillingWindowMapper.ComputeNextHourBoundWindowSlices(evnt, currState, end);

            if (currSlice is null)
            {
                // there are no state machine transitions here so just bail out
                return (Enumerable.Empty<BillingWindowSlice>(), currState);
            }

            return (currSlice, nextState);
        }
    }
}