using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Azure.ServiceBus;
using System.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Test
{
    public class BillingSubmissionServiceTests
    {

        [Fact]
        public async Task SubmitOneBill()
        {
            var controlPlane = new Mock<IControlPlaneInfo>();
            IEnumerable<AzureLocation> locations = new List<AzureLocation>() { AzureLocation.WestUs2 };
            var stampInfo = new Mock<IControlPlaneStampInfo>();
            controlPlane.SetupGet(x => x.Stamp).Returns(stampInfo.Object);
            stampInfo.SetupGet(x => x.DataPlaneLocations).Returns(locations);

            var billingEventManager = new Mock<IBillingEventManager>();
            var planManager = new Mock<IPlanManager>();
            var logger = new Mock<IDiagnosticsLogger>();
            logger.Setup(x => x.WithValues(It.IsAny<LogValueSet>())).Returns(logger.Object);
            var plan = new VsoPlan()
            {
                Plan = new VsoPlanInfo()
                {
                    Name = "PlanName",
                    Location = AzureLocation.WestUs2,
                    ResourceGroup = "RG",
                    Subscription = Guid.NewGuid().ToString(),
                }
            };
            IEnumerable<VsoPlan> plans = new List<VsoPlan>() { plan };
            var endDate = DateTime.Now;
            var usage = new Dictionary<string, double>
            {
                { "meter", 3d }
            };

            var billingSummary = new BillingSummary()
            {
                PeriodEnd = endDate,
                PeriodStart = DateTime.Now,
                Plan = "test",
                SubmissionState = BillingSubmissionState.None,
                Usage = usage,
            };
            var billingEvent = new BillingEvent()
            {
                Plan = plan.Plan,
                Args = billingSummary,
            };
            IEnumerable<BillingEvent> billingSummaries = new List<BillingEvent>() { billingEvent };

            planManager.Setup(x => x.GetBillablePlansByShardAsync(It.IsAny<IEnumerable<AzureLocation>>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(plans));
            billingEventManager.Setup(x => x.GetPlanEventsAsync(It.IsAny<Expression<Func<BillingEvent, bool>>>(), logger.Object)).Returns(Task.FromResult(billingSummaries));
            planManager.Setup(x => x.GetShards()).Returns(new List<string>() { "a" });

            var factory = new Mock<IBillingSubmissionCloudStorageFactory>();
            var client = new Mock<IBillingSubmissionCloudStorageClient>();
            factory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(client.Object));
            client.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult(new BillingSummaryTableSubmission()));
            client.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);

            // Setup a fake lease
            var lease = new Mock<IClaimedDistributedLease>();
            lease.Setup(x => x.Obtain(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(new FakeLease() as IDisposable));
            var sut = new BillingSummarySubmissionService(controlPlane.Object, billingEventManager.Object, logger.Object, factory.Object, lease.Object, new MockTaskHelper(), planManager.Object);
            await sut.ProcessBillingSummariesAsync(new System.Threading.CancellationToken());
            Assert.Equal(BillingSubmissionState.Submitted, billingSummary.SubmissionState);
        }

        [Fact]
        public async Task SubmitFinalBill()
        {
            var controlPlane = new Mock<IControlPlaneInfo>();
            IEnumerable<AzureLocation> locations = new List<AzureLocation>() { AzureLocation.WestUs2 };
            var stampInfo = new Mock<IControlPlaneStampInfo>();
            controlPlane.SetupGet(x => x.Stamp).Returns(stampInfo.Object);
            stampInfo.SetupGet(x => x.DataPlaneLocations).Returns(locations);

            var billingEventManager = new Mock<IBillingEventManager>();
            var planManager = new Mock<IPlanManager>();
            var logger = new Mock<IDiagnosticsLogger>();
            logger.Setup(x => x.WithValues(It.IsAny<LogValueSet>())).Returns(logger.Object);
            var plan1 = new VsoPlan()
            {
                Plan = new VsoPlanInfo()
                {
                    Name = "PlanName1",
                    Location = AzureLocation.WestUs2,
                    ResourceGroup = "RG1",
                    Subscription = Guid.NewGuid().ToString(),
                }
            };
            var plan2 = new VsoPlan()
            {
                Plan = new VsoPlanInfo()
                {
                    Name = "PlanName2",
                    Location = AzureLocation.WestUs2,
                    ResourceGroup = "RG2",
                    Subscription = Guid.NewGuid().ToString(),
                },
                IsDeleted = true
            };
            IEnumerable<VsoPlan> plans = new List<VsoPlan>() { plan1, plan2 };
            IEnumerable<VsoPlan> deletedPlans = new List<VsoPlan>() { plan2 };
            var now = DateTime.UtcNow;
            var absoluteDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
            var periodStart = absoluteDate.Subtract(new TimeSpan(2, 0, 0));
            var periodEnd = absoluteDate.Subtract(new TimeSpan(1, 0, 0));

            var billingSummary1 = new BillingSummary()
            {
                PeriodEnd = periodEnd,
                PeriodStart = periodStart,
                Plan = "plan1",
                SubmissionState = BillingSubmissionState.None,
                Usage = new Dictionary<string, double>
                {
                    { "meter", 3d }
                },
            };
            var billingEvent1 = new BillingEvent()
            {
                Plan = plan1.Plan,
                Args = billingSummary1,
                Type = BillingEventTypes.BillingSummary,
                Time = periodEnd
            };
            var billingSummary2 = new BillingSummary()
            {
                PeriodEnd = periodEnd,
                PeriodStart = periodStart,
                Plan = "plan2",
                SubmissionState = BillingSubmissionState.None,
                PlanIsDeleted = true,
                IsFinalBill = true,
                Usage = new Dictionary<string, double>
                {
                    { "meter", 0d }
                },
            };
            var billingEvent2 = new BillingEvent()
            {
                Plan = plan2.Plan,
                Args = billingSummary2,
                Type = BillingEventTypes.BillingSummary,
                Time = periodEnd
            };
            var billingEvents = new List<BillingEvent>() { billingEvent1, billingEvent2 };
            var queryableBillingEvents = billingEvents.AsQueryable();

            planManager.Setup(x => x.GetBillablePlansByShardAsync(It.IsAny<IEnumerable<AzureLocation>>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(plans));
            billingEventManager.Setup(x => x.GetPlanEventsAsync(It.IsAny<Expression<Func<BillingEvent, bool>>>(), logger.Object)).Returns((Expression<Func<BillingEvent, bool>> xfilter, IDiagnosticsLogger xlogger) =>
            {
                var events = new List<BillingEvent>(queryableBillingEvents.Where(xfilter).OrderBy(x => x.Time));
                return Task.FromResult((IEnumerable<BillingEvent>)events);
            });
            planManager.Setup(x => x.GetShards()).Returns(new List<string>() { "a" });
            planManager.Setup(x => x.UpdateFinalBillSubmittedAsync(It.IsIn(deletedPlans), It.IsAny<IDiagnosticsLogger>())).Returns((VsoPlan xplan, IDiagnosticsLogger xlogger) => { xplan.IsFinalBillSubmitted = true; return Task.FromResult(xplan); });
            planManager.Setup(x => x.UpdateFinalBillSubmittedAsync(It.IsNotIn(deletedPlans), It.IsAny<IDiagnosticsLogger>())).Throws (new Exception ("Tried to submit a final bill for the wrong plan!"));

            var storageFactory = new Mock<IBillingSubmissionCloudStorageFactory>();
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();
            storageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult(new BillingSummaryTableSubmission()));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);

            // Setup a fake lease
            var lease = new Mock<IClaimedDistributedLease>();
            lease.Setup(x => x.Obtain(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(new FakeLease() as IDisposable));
            var sut = new BillingSummarySubmissionService(controlPlane.Object, billingEventManager.Object, logger.Object, storageFactory.Object, lease.Object, new MockTaskHelper(), planManager.Object);
            await sut.ProcessBillingSummariesAsync(new System.Threading.CancellationToken());
            Assert.Equal(BillingSubmissionState.Submitted, billingSummary1.SubmissionState);
            Assert.Equal(BillingSubmissionState.NeverSubmit, billingSummary2.SubmissionState);
            Assert.False(plan1.IsFinalBillSubmitted);
            Assert.True(plan2.IsFinalBillSubmitted);
        }

        [Fact]
        public async Task CheckForBillErrors()
        {
            var controlPlane = new Mock<IControlPlaneInfo>();
            IEnumerable<AzureLocation> locations = new List<AzureLocation>() { AzureLocation.WestUs2 };
            var stampInfo = new Mock<IControlPlaneStampInfo>();
            controlPlane.SetupGet(x => x.Stamp).Returns(stampInfo.Object);
            stampInfo.SetupGet(x => x.DataPlaneLocations).Returns(locations);

            var billingEventManager = new Mock<IBillingEventManager>();
            var logger = new Mock<IDiagnosticsLogger>();
            logger.Setup(x => x.WithValues(It.IsAny<LogValueSet>())).Returns(logger.Object);
            var planManager = new Mock<IPlanManager>();
            var plan = new VsoPlan()
            {
                Plan = new VsoPlanInfo()
                {
                    Name = "PlanName",
                    Location = AzureLocation.WestUs2,
                    ResourceGroup = "RG",
                    Subscription = Guid.NewGuid().ToString(),
                }
            };
            IEnumerable<VsoPlan> plans = new List<VsoPlan>() { plan };

            var endDate = DateTime.Now;
            var usage = new Dictionary<string, double>
            {
                { "meter", 3d }
            };

            var billingSummary = new BillingSummary()
            {
                PeriodEnd = endDate,
                PeriodStart = DateTime.Now,
                Plan = "test",
                SubmissionState = BillingSubmissionState.None,
                Usage = usage,
                EventId = "EventID",

            };
            var billingEvent = new BillingEvent()
            {
                Plan = plan.Plan,
                Args = billingSummary,
            };
            IEnumerable<BillingEvent> billingSummaries = new List<BillingEvent>() { billingEvent };
            billingEventManager.Setup(x => x.GetPlanEventsAsync(It.IsAny<Expression<Func<BillingEvent, bool>>>(), logger.Object)).Returns(Task.FromResult(billingSummaries));

            var errorResults = new BillSubmissionErrorResult()
            {
                PartitionKey = "testPartitionKey",
                RowKey = "testRowKey",
                UsageRecordPartitionKey = "testOldRecordBatchID",
                UsageRecordRowKey = "testOldEventID"
            };
            IEnumerable<BillSubmissionErrorResult> listOfErrors = new List<BillSubmissionErrorResult>() { errorResults };


            // Set up storage
            var factory = new Mock<IBillingSubmissionCloudStorageFactory>();
            var client = new Mock<IBillingSubmissionCloudStorageClient>();
            factory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(client.Object));
            client.Setup(x => x.CheckForErrorsOnQueue()).Returns(Task.FromResult(true));
            client.Setup(x => x.GetSubmissionErrors()).Returns(Task.FromResult(listOfErrors));

            // Setup a fake lease
            var lease = new Mock<IClaimedDistributedLease>();
            lease.Setup(x => x.Obtain(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(new FakeLease() as IDisposable));

            var sut = new BillingSummarySubmissionService(controlPlane.Object, billingEventManager.Object, logger.Object, factory.Object, lease.Object, new MockTaskHelper(), planManager.Object );
            await sut.CheckForBillingSubmissionErorrs(new System.Threading.CancellationToken());
            Assert.Equal(BillingSubmissionState.Error, billingSummary.SubmissionState);
        }
    }
}
