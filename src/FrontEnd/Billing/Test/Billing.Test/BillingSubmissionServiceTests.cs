using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

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

            planManager.Setup(x => x.GetPlansByShardAsync(It.IsAny<IEnumerable<AzureLocation>>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(plans));
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

        class MockTaskHelper : ITaskHelper
        {
            public Task<bool> RetryUntilSuccessOrTimeout(string name, Func<Task<bool>> callback, TimeSpan timeoutTimeSpan, TimeSpan? waitTimeSpan = null, IDiagnosticsLogger logger = null, Action onTimeout = null)
            {
                throw new NotImplementedException();
            }

            public Task<bool> RetryUntilSuccessOrTimeout(string name, Func<IDiagnosticsLogger, Task<bool>> callback, TimeSpan timeoutTimeSpan, TimeSpan? waitTimeSpan = null, IDiagnosticsLogger logger = null, Action onTimeout = null)
            {
                throw new NotImplementedException();
            }

            public void RunBackground(string name, Func<IDiagnosticsLogger, Task> callback, IDiagnosticsLogger logger = null, bool autoLogOperation = true, Action<Exception, IDiagnosticsLogger> errCallback = null, TimeSpan? delay = null)
            {
                throw new NotImplementedException();
            }

            public void RunBackgroundConcurrentEnumerable<T>(string name, IEnumerable<T> list, Func<T, IDiagnosticsLogger, Task> callback, IDiagnosticsLogger logger = null, Func<T, IDiagnosticsLogger, Task<IDisposable>> obtainLease = null, Action<T, Exception, IDiagnosticsLogger> errItemCallback = null, int concurrentLimit = 3, int successDelay = 250)
            {
                callback(list.First(), logger);
            }

            public Task RunConcurrentEnumerableAsync<T>(string name, IEnumerable<T> list, Func<T, IDiagnosticsLogger, Task> callback, IDiagnosticsLogger logger = null, Func<T, IDiagnosticsLogger, Task<IDisposable>> obtainLease = null, Action<T, Exception, IDiagnosticsLogger> errItemCallback = null, int concurrentLimit = 3, int successDelay = 250)
            {
                return callback(list.First(), logger);
            }

            public void RunBackgroundLong(string name, Func<IDiagnosticsLogger, Task> callback, IDiagnosticsLogger logger = null, bool autoLogOperation = true, Action<Exception, IDiagnosticsLogger> errCallback = null, TimeSpan? delay = null)
            {
                throw new NotImplementedException();
            }

            public void RunBackgroundLoop(string name, Func<IDiagnosticsLogger, Task<bool>> callback, TimeSpan? schedule = null, IDiagnosticsLogger logger = null, bool autoLogLoopOperation = false, Func<Exception, IDiagnosticsLogger, bool> errLoopCallback = null)
            {
                throw new NotImplementedException();
            }

            public Task RunBackgroundLoopAsync(string name, Func<IDiagnosticsLogger, Task<bool>> callback, TimeSpan? schedule = null, IDiagnosticsLogger logger = null, bool autoLogLoopOperation = false, Func<Exception, IDiagnosticsLogger, bool> errLoopCallback = null)
            {
                throw new NotImplementedException();
            }

            public void RunBackgroundEnumerable<T>(string name, IEnumerable<T> list, Func<T, IDiagnosticsLogger, Task> callback, IDiagnosticsLogger logger = null, Func<T, IDiagnosticsLogger, Task<IDisposable>> obtainLease = null, int itemDelay = 250)
            {
                throw new NotImplementedException();
            }

            public Task RunEnumerableAsync<T>(string name, IEnumerable<T> list, Func<T, IDiagnosticsLogger, Task> callback, IDiagnosticsLogger logger = null, Func<T, IDiagnosticsLogger, Task<IDisposable>> obtainLease = null, int itemDelay = 250)
            {
                throw new NotImplementedException();
            }
        }
    }
}
