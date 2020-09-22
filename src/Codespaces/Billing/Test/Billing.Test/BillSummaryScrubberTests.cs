using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks.Payloads;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Test
{
    public class BillSummaryScrubberTests
    {
        private Mock<BillingSettings> BillingSettings { get; }

        private Mock<IBillSummaryManager> BillSummaryManager { get; }

        private Mock<IEnvironmentStateChangeManager> EnvironmentStateChangeManager { get; }

        private Mock<IBillingArchivalManager> BillingArchivalManager { get; }

        private static readonly IDiagnosticsLogger Logger = new DefaultLoggerFactory().New();

        private BillSummaryScrubber BillSummaryScrubber { get; }
        public BillSummaryScrubberTests()
        {
            BillSummaryManager = new Mock<IBillSummaryManager>();
            EnvironmentStateChangeManager = new Mock<IEnvironmentStateChangeManager>();
            BillingArchivalManager = new Mock<IBillingArchivalManager>();
            BillSummaryScrubber = new BillSummaryScrubber(
                BillSummaryManager.Object,
                EnvironmentStateChangeManager.Object,
                BillingArchivalManager.Object);
        }

        private readonly VsoPlanInfo PlanInfo = new VsoPlanInfo
        {
            Subscription = "59244c50-8b22-47dd-b6fe-0019b048fef6",
            ResourceGroup = "resource-group",
            Name = "plan-name"
        };

        private readonly string EnvId = "2f86cf59-a5d8-4e37-a450-26a42d63603a";
        private readonly string PlanId = "c56c1aa6-d6f4-43a5-a9fc-addbfa6be1d2";

        [Fact]
        public async Task ScrubBillSummariesForPlanAsync_SummaryHasEnvWithNoEvents_RemovesUsageDetail()
        {
            // Setup
            var request = new BillScrubberRequest
            {
                PlanId = PlanId,
                DesiredEndTime = new DateTime(2020, 6, 1),
                CheckForFinalStates = true,
            };

            var envUsage = new EnvironmentUsage
            {
                Id = EnvId
            };

            var latestBillSummary = new BillSummary
            {
                Plan = PlanInfo,
                PeriodEnd = new DateTime(2020, 6, 1),
                UsageDetail = new List<EnvironmentUsage> { envUsage }
            };
            BillSummaryManager
                .Setup(x => x.GetAllSummaries(request.PlanId, request.DesiredEndTime, It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(new List<BillSummary> { latestBillSummary }.AsEnumerable()));

            // Must return empty list to trigger scrubber
            EnvironmentStateChangeManager
                .Setup(x => x.GetAllStateChanges(request.PlanId, request.DesiredEndTime, It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(new List<EnvironmentStateChange> { }.AsEnumerable()));

            // Run
            await BillSummaryScrubber.ScrubBillSummariesForPlanAsync(request, Logger);

            // Verify
            BillSummaryManager.Verify(
                x => x.CreateOrUpdateAsync(It.Is<BillSummary>(b => !b.UsageDetail.Contains(envUsage)), It.IsAny<IDiagnosticsLogger>()),
                Times.Once);
        }

        [Fact]
        public async Task ScrubBillSummariesForPlanAsync_SummaryHasEnvWithEvents_NoChangeToBillSummary()
        {
            // Setup
            var finalState = nameof(CloudEnvironmentState.Deleted);

            var request = new BillScrubberRequest
            {
                PlanId = PlanId,
                DesiredEndTime = new DateTime(2020, 6, 1),
                CheckForFinalStates = true,
            };

            var envUsage = new EnvironmentUsage
            {
                Id = EnvId,
                EndState = finalState
            };

            var latestBillSummary = new BillSummary
            {
                Plan = PlanInfo,
                PeriodEnd = new DateTime(2020, 6, 1),
                UsageDetail = new List<EnvironmentUsage> { envUsage }
            };
            BillSummaryManager
                .Setup(x => x.GetAllSummaries(request.PlanId, request.DesiredEndTime, It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(new List<BillSummary> { latestBillSummary }.AsEnumerable()));

            // Must have an envEvent with same env ID and time before PeriodEnd of latest bill
            var envEvent = new EnvironmentStateChange
            {
                Time = new DateTime(2020, 5, 1),
                Environment = new EnvironmentBillingInfo { Id = EnvId },
                NewValue = finalState
            };

            EnvironmentStateChangeManager
                .Setup(x => x.GetAllStateChanges(request.PlanId, request.DesiredEndTime, It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(new List<EnvironmentStateChange> { envEvent }.AsEnumerable()));

            // Run
            await BillSummaryScrubber.ScrubBillSummariesForPlanAsync(request, Logger);

            // Verify
            BillSummaryManager.Verify(
                x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>()),
                Times.Never);
        }

        [Fact]
        public async Task ScrubBillSummariesForPlanAsync_ChangeInFinalState_BillSummaryIsUpdated()
        {
            // Setup
            var initialEndState = nameof(CloudEnvironmentState.Shutdown);
            var finalState = nameof(CloudEnvironmentState.Deleted);

            var request = new BillScrubberRequest
            {
                PlanId = PlanId,
                DesiredEndTime = new DateTime(2020, 6, 1),
                CheckForFinalStates = true,
            };

            var envUsage = new EnvironmentUsage
            {
                Id = EnvId,
                EndState = initialEndState
            };

            var latestBillSummary = new BillSummary
            {
                Plan = PlanInfo,
                PeriodEnd = new DateTime(2020, 6, 1),
                UsageDetail = new List<EnvironmentUsage> { envUsage }
            };
            BillSummaryManager
                .Setup(x => x.GetAllSummaries(request.PlanId, request.DesiredEndTime, It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(new List<BillSummary> { latestBillSummary }.AsEnumerable()));

            // Must have an envEvent with same env ID and a different NewValue than the one in the BillSummary
            var envEvent = new EnvironmentStateChange
            {
                Time = new DateTime(2020, 5, 1),
                Environment = new EnvironmentBillingInfo { Id = EnvId },
                NewValue = finalState,
            };

            EnvironmentStateChangeManager
                .Setup(x => x.GetAllStateChanges(request.PlanId, request.DesiredEndTime, It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(new List<EnvironmentStateChange> { envEvent }.AsEnumerable()));

            // Run
            await BillSummaryScrubber.ScrubBillSummariesForPlanAsync(request, Logger);

            // Verify
            BillSummaryManager.Verify(
                x => x.CreateOrUpdateAsync(It.Is<BillSummary>(b => b.UsageDetail.First().EndState == finalState), It.IsAny<IDiagnosticsLogger>()),
                Times.Once);
        }
    }
}