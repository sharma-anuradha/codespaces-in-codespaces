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
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Test
{
    public class GitHubServiceTests
    {
        private const string Gh = "gh";
        private const string GitHub = "github";
        private const string GitHubSubscription = "d833c9b9-c971-47f1-8156-c4236552bdfd";

        public enum Missing
        {
            None,
            UsageDetail,
            Environments,
            ResourceUsage,
            Lists
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(0, 1)]
        [InlineData(0, 0)]
        [InlineData(0, 0, Missing.UsageDetail)]
        [InlineData(0, 0, Missing.Environments)]
        [InlineData(0, 0, Missing.ResourceUsage)]
        [InlineData(0, 0, Missing.Lists)]
        public async void SubmitPartnerInvoice(
            int compute,
            int storage,
            Missing missing = default)
        {
            var controlPlane = new Mock<IControlPlaneInfo>();
            var stampInfo = new Mock<IControlPlaneStampInfo>();
            var planManager = new Mock<IPlanManager>();
            var logger = new Mock<IDiagnosticsLogger>();
            var billingEventManager = new Mock<IBillingEventManager>();
            var factory = new Mock<IPartnerCloudStorageFactory>();
            var client = new Mock<IPartnerCloudStorageClient>();
            var lease = new Mock<IClaimedDistributedLease>();
            var taskHelper = new MockTaskHelper();

            controlPlane
                .SetupGet(x => x.Stamp)
                .Returns(stampInfo.Object);

            stampInfo
                .SetupGet(x => x.DataPlaneLocations)
                .Returns(new[] { AzureLocation.WestUs2 });

            logger
                .Setup(x => x.WithValues(It.IsAny<LogValueSet>()))
                .Returns(logger.Object);

            var planInfo = new VsoPlanInfo()
            {
                Name = "PlanName",
                Location = AzureLocation.WestUs2,
                ResourceGroup = "RG",
                Subscription = GitHubSubscription,
            };

            var plan = new VsoPlan()
            {
                Plan = planInfo,
                Partner = Partner.GitHub
            };

            var usage = new Dictionary<string, double>
            {
                { "meter", 3d }
            };

            var resourceUsage = new ResourceUsageDetail()
            {
                Compute = missing == Missing.Lists ? null : new List<ComputeUsageDetail>() { new ComputeUsageDetail() { Usage = compute } },
                Storage = missing == Missing.Lists ? null : new List<StorageUsageDetail>() { new StorageUsageDetail() { Usage = storage } }
            };

            var environmentUsageDetail = new EnvironmentUsageDetail()
            {
                Name = "FooBar",
                UserId = Guid.NewGuid().ToString(),
                ResourceUsage = missing == Missing.ResourceUsage ? null : resourceUsage,
            };

            var usageDetail = new UsageDetail()
            {
                Environments = missing == Missing.Environments ? null :
                    new Dictionary<string, EnvironmentUsageDetail>()
                    {
                        {  Guid.NewGuid().ToString(), environmentUsageDetail },
                    }
            };

            var billingSummary = new BillingSummary()
            {
                PeriodEnd = DateTime.Now,
                PeriodStart = DateTime.Now,
                Plan = "test",
                SubmissionState = BillingSubmissionState.None,
                Usage = usage,
                UsageDetail = missing == Missing.UsageDetail ? null : usageDetail,
            };

            var billingEvent = new BillingEvent()
            {
                Plan = planInfo,
                Args = billingSummary,
            };

            var partnerQueueSubmission = new PartnerQueueSubmission(billingEvent);

            var isEmptySubmission = (missing != default) || (compute == 0 && storage == 0);
            Assert.True(partnerQueueSubmission.IsEmpty() == isEmptySubmission);

            planManager.Setup(x => 
                x.GetPartnerPlansByShardAsync(
                    It.IsAny<IEnumerable<AzureLocation>>(), 
                    It.IsAny<string>(), 
                    It.IsAny<Partner>(),
                    It.IsAny<IDiagnosticsLogger>())
                ).Returns(Task.FromResult(new[] { plan } as IEnumerable<VsoPlan>)
            );

            billingEventManager.Setup(x => 
                x.GetPlanEventsAsync(
                    It.IsAny<Expression<Func<BillingEvent, bool>>>(), logger.Object)
                ).Returns(Task.FromResult(new[] { billingEvent } as IEnumerable<BillingEvent>)
            );

            planManager.Setup(x => 
                x.GetShards()
            ).Returns(new List<string>() { GitHubSubscription[0].ToString() });

            factory.Setup(x => x.CreatePartnerCloudStorage(
                It.IsAny<AzureLocation>(), Gh)
            ).Returns(Task.FromResult(client.Object));

            client.Setup(x =>
                x.PushPartnerQueueSubmission(
                    It.IsAny<PartnerQueueSubmission>()
                )
            ).Returns(Task.FromResult(partnerQueueSubmission));

            // Setup a fake lease
            lease.Setup(x =>
                x.Obtain(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<IDiagnosticsLogger>()
                )
            ).Returns(Task.FromResult(new FakeLease() as IDisposable));

            var sut = new GitHubService(
                controlPlane.Object,
                billingEventManager.Object,
                logger.Object,
                factory.Object,
                lease.Object,
                taskHelper,
                planManager.Object
            );

            await sut.Execute(new System.Threading.CancellationToken());

            var expectedBillingSubmissionState = isEmptySubmission ?
                BillingSubmissionState.NeverSubmit : BillingSubmissionState.Submitted;
            Assert.Equal(expectedBillingSubmissionState, billingSummary.PartnerSubmissionState);
        }
    }
}
