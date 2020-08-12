// <copyright file="BillingEventToBillingWindowMapper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <inheritdoc/>
    public class BillingEventToBillingWindowMapper : IBillingEventToBillingWindowMapper
    {
        /// <inheritdoc/>
        public (IEnumerable<BillingWindowSlice> Slices, BillingWindowSlice.NextState NextState) ComputeNextHourBoundWindowSlices(
            BillingEvent currentEvent,
            BillingWindowSlice.NextState currentState,
            DateTime endTimeForPeriod)
        {
            var (currSlice, nextState) = CalculateNextWindow(currentEvent, currentState, endTimeForPeriod);
            IEnumerable<BillingWindowSlice> slices = null;

            if (currSlice != null)
            {
                slices = GenerateHourBoundTimeSlices(currSlice);
            }

            return (slices, nextState);
        }

        private (BillingWindowSlice CurrState, BillingWindowSlice.NextState NextState) CalculateNextWindowForNullEvent(BillingWindowSlice.NextState currentState, DateTime endTimeForPeriod)
        {
            // No new time slice to create if last status was Deleted.
            // There will be no more events after a Deleted event.
            if (currentState.EnvironmentState == CloudEnvironmentState.Available || currentState.EnvironmentState == CloudEnvironmentState.Shutdown)
            {
                var finalSlice = new BillingWindowSlice()
                {
                    StartTime = currentState.TransitionTime,
                    EndTime = endTimeForPeriod,

                    BillingState = currentState.EnvironmentState == CloudEnvironmentState.Shutdown ?
                        BillingWindowBillingState.Inactive : BillingWindowBillingState.Active,
                    Sku = currentState.Sku,
                };

                // currentState is also the next state here because it is still active as we have no more transition events
                return (finalSlice, currentState);
            }
            else
            {
                return (null, null);
            }
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

        /// <summary>
        /// Helper method to extract state transition settings from a given BillingEvent.
        /// </summary>
        /// <param name="billingEvent">The billing event.</param>
        /// <returns>A NextState which contains state transition metadata from the event.</returns>
        private BillingWindowSlice.NextState BuildNextStateFromEvent(BillingEvent billingEvent)
        {
            return new BillingWindowSlice.NextState
            {
                EnvironmentState = ParseEnvironmentState(((BillingStateChange)billingEvent.Args).NewValue),
                Sku = billingEvent.Environment.Sku,
                TransitionTime = billingEvent.Time,
            };
        }

        /// <summary>
        /// Helper method to extract state transition settings from a given BillingEvent.
        /// </summary>
        /// <summary>
        /// This method creates the class BillingWindowSlice<see cref="BillingWindowSlice"/>.
        /// A BillingWindowSlice represents a timespan of billing activity from one
        /// CloudEnvironmentState<see cref="CloudEnvironmentState"/> to the next. It contains all information
        /// neccessary to create a unit of billing for its period.
        /// </summary>
        /// <param name="currentEvent">The current event being processed.</param>
        /// <param name="currentState">the current state (sku, time) for the state machine.</param>
        /// <param name="endTimeForPeriod">the overall end time for the period.</param>
        /// <returns>A tuple representing the next slice/state.</returns>
        private (
            BillingWindowSlice CurrState,
            BillingWindowSlice.NextState NextState) CalculateNextWindow(BillingEvent currentEvent, BillingWindowSlice.NextState currentState, DateTime endTimeForPeriod)
        {
            // If we're looking at a null current Event and the last event status is not Deleted,
            // just calculate the time delta otherwise, run through the state machine again.
            if (currentEvent == null)
            {
                return CalculateNextWindowForNullEvent(currentState, endTimeForPeriod);
            }

            var nextState = BuildNextStateFromEvent(currentEvent);

            BillingWindowSlice nextSlice = null;
            switch (currentState.EnvironmentState)
            {
                case CloudEnvironmentState.Available:
                    switch (nextState.EnvironmentState)
                    {
                        // CloudEnvironmentState has gone from Available to Shutdown or Deleted
                        // in this BillingWindowSlice.
                        case CloudEnvironmentState.Shutdown:
                        case CloudEnvironmentState.Deleted:
                            nextSlice = new BillingWindowSlice()
                            {
                                StartTime = currentState.TransitionTime,
                                EndTime = currentEvent.Time,
                                BillingState = BillingWindowBillingState.Active,
                                Sku = currentState.Sku,
                            };
                            break;
                        default:
                            break;
                    }

                    break;

                case CloudEnvironmentState.Shutdown:
                    switch (nextState.EnvironmentState)
                    {
                        // CloudEnvironmentState has gone from Shutdown to Available or Deleted
                        // in this BillingWindowSlice.
                        case CloudEnvironmentState.Available:
                        case CloudEnvironmentState.Deleted:
                        // Environment's state hasn't changed, but some other setting was updated so we still need to create a slice
                        case CloudEnvironmentState.Shutdown:
                            nextSlice = new BillingWindowSlice()
                            {
                                StartTime = currentState.TransitionTime,
                                EndTime = currentEvent.Time,
                                BillingState = BillingWindowBillingState.Inactive,
                                Sku = currentState.Sku,
                            };
                            break;
                        default:
                            break;
                    }

                    break;

                // No previous billing summary
                case CloudEnvironmentState.None:
                    switch (nextState.EnvironmentState)
                    {
                        case CloudEnvironmentState.Available:
                            nextSlice = new BillingWindowSlice
                            {
                                // Setting StartTime = EndTime because this time slice should
                                // not be billed. This represent the slice of time from
                                // Provisioning to Available, in which we won't bill.
                                // The next time slice will bill for Available time.
                                StartTime = currentEvent.Time,
                                EndTime = currentEvent.Time,
                                BillingState = BillingWindowBillingState.Inactive,
                                Sku = currentState.Sku,
                            };
                            break;
                    }

                    break;
                default:
                    break;
            }

            return (nextSlice, nextState);
        }

        private IEnumerable<BillingWindowSlice> GenerateHourBoundTimeSlices(BillingWindowSlice currSlice)
        {
            var slices = new List<BillingWindowSlice>();
            var nextHourBoundary = new DateTime(currSlice.StartTime.Year, currSlice.StartTime.Month, currSlice.StartTime.Day, currSlice.StartTime.Hour, 0, 0, DateTimeKind.Utc).AddHours(1);
            if (currSlice.EndTime <= nextHourBoundary)
            {
                slices.Add(currSlice);
            }
            else
            {
                var endTime = nextHourBoundary;
                var dividedFully = false;

                // create the input time slice and add it to the hour boundary.
                var lastSlice = new BillingWindowSlice()
                {
                    StartTime = currSlice.StartTime,
                    EndTime = endTime,
                    BillingState = currSlice.BillingState,
                    Sku = currSlice.Sku,
                };
                slices.Add(lastSlice);

                while (!dividedFully)
                {
                    endTime = endTime.AddHours(1);
                    if (currSlice.EndTime > endTime)
                    {
                        // We need to loop again and add a new slice for this hour segment since this segment is a subset.
                        lastSlice = new BillingWindowSlice()
                        {
                            StartTime = lastSlice.EndTime,
                            EndTime = endTime,
                            BillingState = currSlice.BillingState,
                            Sku = currSlice.Sku,
                        };
                        slices.Add(lastSlice);
                    }
                    else
                    {
                        // Get the remainder of the time
                        dividedFully = true;
                        lastSlice = new BillingWindowSlice()
                        {
                            StartTime = lastSlice.EndTime,
                            EndTime = currSlice.EndTime,
                            BillingState = currSlice.BillingState,
                            Sku = currSlice.Sku,
                        };
                        slices.Add(lastSlice);
                    }
                }
            }

            return slices;
        }
    }
}
