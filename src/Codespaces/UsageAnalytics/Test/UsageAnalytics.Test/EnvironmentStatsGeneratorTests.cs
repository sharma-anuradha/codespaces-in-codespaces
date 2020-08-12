using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UsageAnalytics
{
    /// <summary>
    /// Tests for <see cref="EnvironmentStatsGenerator"/>
    /// </summary>
    public class EnvironmentStatsGeneratorTest
    {

        private static readonly BillingEventToBillingWindowMapper BillingWindowMapper = new BillingEventToBillingWindowMapper();
        private static readonly IDiagnosticsLogger Logger = new DefaultLoggerFactory().New();

        [Fact]
        public async Task ComputigStatsOfAnEmptyListReturnsZeroValuesAsync()
        {
            var environmentStatGenerator = new EnvironmentStatsGenerator(BillingWindowMapper);
            var billingEvents = Enumerable.Empty<BillingEvent>();
            var endDate = DateTime.UtcNow;

            var stats = await environmentStatGenerator.GetStats(billingEvents, endDate, Logger);

            Assert.Equal(0, stats.AverageTimeToShutdown);
            Assert.Equal(0, stats.AverageTimeToNextUse);
            Assert.Equal(0, stats.MaxTimeToNextUse);
            Assert.Equal(0, stats.TotalTimeSuspend);
            Assert.Equal(0, stats.TotalTimeActive);
            Assert.Equal(0, stats.NumberOfTimesActive);
            Assert.Equal(0, stats.NumberOfTimeShutdown);
        }

        [Fact]
        public async Task StatsAreComputedCorrectlyWhenAnEnvironmentIsStillShutdown()
        {
            var environmentStatGenerator = new EnvironmentStatsGenerator(BillingWindowMapper);
            var startDate = new DateTime(2020, 1, 1);
            var endDate = DateTime.UtcNow;
            (double hoursSpentInState, CloudEnvironmentState state)[] states = {
                (1, CloudEnvironmentState.Available),
                (5, CloudEnvironmentState.Shutdown),
                (1, CloudEnvironmentState.Available),
                (2, CloudEnvironmentState.Shutdown),
                (4, CloudEnvironmentState.Available),
                (4, CloudEnvironmentState.Shutdown),
                (3, CloudEnvironmentState.Available),
                (1, CloudEnvironmentState.Shutdown),
            };
            var billingEvents = GenerateBillingEvents(startDate, states.ToList());

            var stats = await environmentStatGenerator.GetStats(billingEvents, endDate, Logger);

            VerifyStats(startDate, endDate, states, stats);
        }

        [Fact]
        public async Task StatsAreComputedCorrectlyWhenAnEnvironmentIsStillActive()
        {
            var environmentStatGenerator = new EnvironmentStatsGenerator(BillingWindowMapper);
            var startDate = new DateTime(2020, 1, 1);
            var endDate = DateTime.UtcNow;
            (double hoursSpentInState, CloudEnvironmentState state)[] states = {
                (1, CloudEnvironmentState.Available),
                (65, CloudEnvironmentState.Shutdown),
                (23, CloudEnvironmentState.Available),
                (67, CloudEnvironmentState.Shutdown),
                (40, CloudEnvironmentState.Available),
                (44, CloudEnvironmentState.Shutdown),
                (37, CloudEnvironmentState.Available),
            };
            var billingEvents = GenerateBillingEvents(startDate, states.ToList());

            var stats = await environmentStatGenerator.GetStats(billingEvents, endDate, Logger);

            VerifyStats(startDate, endDate, states, stats);
        }

        private void VerifyStats(
            DateTime startDate,
            DateTime endDate,
            (double hoursSpentInState, CloudEnvironmentState state)[] states,
            EnvironmentStats stats)
        {
            var shutdowns = states.Where(s => s.state == CloudEnvironmentState.Shutdown).Select(s => s.hoursSpentInState);
            var availables = states.Where(s => s.state == CloudEnvironmentState.Available).Select(s => s.hoursSpentInState);
            var completedShutdowns = shutdowns;
            var completedAvailables = availables;

            if (states.Last().state == CloudEnvironmentState.Shutdown)
            {
                completedShutdowns = shutdowns.Take(shutdowns.Count() - 1);
            }


            if (states.Last().state == CloudEnvironmentState.Available)
            {
                completedAvailables = availables.Take(availables.Count() - 1);
            }


            Assert.Equal(completedAvailables.Average(), stats.AverageTimeToShutdown);
            Assert.Equal(completedShutdowns.Average(), stats.AverageTimeToNextUse);
            Assert.Equal(shutdowns.Max(), stats.MaxTimeToNextUse);
            Assert.Equal((endDate - startDate).TotalHours - stats.TotalTimeActive, stats.TotalTimeSuspend);
            Assert.Equal((endDate - startDate).TotalHours - stats.TotalTimeSuspend, stats.TotalTimeActive);
            Assert.Equal(availables.Count(), stats.NumberOfTimesActive);
            Assert.Equal(shutdowns.Count(), stats.NumberOfTimeShutdown);
        }

        /// <summary>
        /// Takes a list of state transitions, and generates a list of billing events.
        /// </summary>
        /// <param name="startDate">The time of the first event.</param>
        /// <param name="transitions">A pair of the state duration - state,</param>
        /// <returns>A list of billing events.</returns>
        private IEnumerable<BillingEvent> GenerateBillingEvents(
            DateTime startDate,
            IEnumerable<(double hoursSpentInState, CloudEnvironmentState state)> transitions)
        {
            var previousState = CloudEnvironmentState.None;
            var previousTime = startDate;
            var hoursSpentInPreviousState = 0.0;

            return transitions.Select(t =>
            {
                var ev = new BillingEvent
                {
                    Args = new BillingStateChange { OldValue = previousState.ToString(), NewValue = t.state.ToString() },
                    Type = BillingEventTypes.EnvironmentStateChange,
                    Time = previousTime.AddHours(hoursSpentInPreviousState),
                    Environment = new Mock<EnvironmentBillingInfo>().Object
                };

                previousTime = ev.Time;
                previousState = t.state;
                hoursSpentInPreviousState = t.hoursSpentInState;

                return ev;
            });
        }
    }

}
