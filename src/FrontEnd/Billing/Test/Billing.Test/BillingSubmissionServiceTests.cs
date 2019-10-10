using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Accounts;
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
            Mock<IControlPlaneInfo> controlPlane = new Mock<IControlPlaneInfo>();
            IEnumerable<AzureLocation> locations = new List<AzureLocation>() { AzureLocation.WestUs2 };
            controlPlane.Setup(x => x.GetAllDataPlaneLocations()).Returns(locations);

            Mock<IBillingEventManager> billingEventManager = new Mock<IBillingEventManager>();
            Mock<IDiagnosticsLogger> logger = new Mock<IDiagnosticsLogger>();
            var vsoAccount = new VsoAccountInfo()
            {
                Name = "AccountName",
                Location = AzureLocation.WestUs2,
                ResourceGroup = "RG",
                Subscription = Guid.NewGuid().ToString(),
            };
            IEnumerable<VsoAccountInfo> accounts = new List<VsoAccountInfo>() { vsoAccount };
            var endDate = DateTime.Now;
            var usage = new Dictionary<string, double>();
            usage.Add("meter", 3d);

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
                Account = vsoAccount,
                Args = billingSummary,
            };
            IEnumerable<BillingEvent> billingSummaries = new List<BillingEvent>() { billingEvent };
            billingEventManager.Setup(x => x.GetAccountsByShardAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), It.IsAny<ICollection<AzureLocation>>(), It.IsAny<string>())).Returns(Task.FromResult(accounts));
            billingEventManager.Setup(x => x.GetAccountEventsAsync(It.IsAny<Expression<Func<BillingEvent, bool>>>(), logger.Object)).Returns(Task.FromResult(billingSummaries));

            Mock<IBillingSubmissionCloudStorageFactory> factory = new Mock<IBillingSubmissionCloudStorageFactory>();
            Mock<IBillingSubmissionCloudStorageClient> client = new Mock<IBillingSubmissionCloudStorageClient>();
            factory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(client.Object));
            client.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult(new BillingSummaryTableSubmission()));
            client.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);

            // Setup a fake lease
            Mock<IClaimedDistributedLease> lease = new Mock<IClaimedDistributedLease>();
            lease.Setup(x => x.Obtain(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(new FakeLease() as IDisposable));
            BillingSummarySubmissionService sut = new BillingSummarySubmissionService(controlPlane.Object, billingEventManager.Object, logger.Object, factory.Object, lease.Object, new MockTaskHelper());
            await sut.ProcessBillingSummaries();
            Assert.Equal(BillingSubmissionState.Submitted, billingSummary.SubmissionState);
        }

        class FakeLease : IDisposable
        {
            public void Dispose()
            {
            }
        }

        class MockTaskHelper : ITaskHelper
        {
            public Task<bool> RetryUntilSuccessOrTimeout(string name, Func<Task<bool>> callback, TimeSpan timeoutTimeSpan, TimeSpan? waitTimeSpan = null, IDiagnosticsLogger logger = null, Action onTimeout = null)
            {
                throw new NotImplementedException();
            }

            public void RunBackground(string name, Func<IDiagnosticsLogger, Task> callback, IDiagnosticsLogger logger = null, bool autoLogOperation = true, Action<Exception> errCallback = null, TimeSpan? delay = null)
            {
                throw new NotImplementedException();
            }

            public void RunBackgroundEnumerable<T>(string name, IEnumerable<T> list, Func<T, IDiagnosticsLogger, Task> callback, IDiagnosticsLogger logger = null, Func<T, IDiagnosticsLogger, Task<IDisposable>> obtainLease = null, Action<T, Exception> errItemCallback = null, int concurrentLimit = 3, int successDelay = 250, int failDelay = 100)
            {
                callback(list.First(), logger);
            }

            public Task RunBackgroundEnumerableAsync<T>(string name, IEnumerable<T> list, Func<T, IDiagnosticsLogger, Task> callback, IDiagnosticsLogger logger = null, Func<T, IDiagnosticsLogger, Task<IDisposable>> obtainLease = null, Action<T, Exception> errItemCallback = null, int concurrentLimit = 3, int successDelay = 250, int failDelay = 100)
            {
                return callback(list.First(), logger);
            }

            public void RunBackgroundLong(string name, Func<IDiagnosticsLogger, Task> callback, IDiagnosticsLogger logger = null, bool autoLogOperation = true, Action<Exception> errCallback = null, TimeSpan? delay = null)
            {
                throw new NotImplementedException();
            }

            public void RunBackgroundLoop(string name, Func<IDiagnosticsLogger, Task<bool>> callback, TimeSpan? schedule = null, IDiagnosticsLogger logger = null, bool autoLogLoopOperation = false, Func<Exception, bool> errLoopCallback = null)
            {
                throw new NotImplementedException();
            }
        }
    }
}
