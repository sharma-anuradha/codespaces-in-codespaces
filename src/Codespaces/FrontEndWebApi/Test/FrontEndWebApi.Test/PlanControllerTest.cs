using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Test
{
    public class PlanControllerTest
    {
        public PlanControllerTest()
        {
            var planSettings = new PlanManagerSettings() { DefaultMaxPlansPerSubscription = 20 };

            var mockSystemConfiguration = new Mock<ISystemConfiguration>();
            mockSystemConfiguration
                .Setup(x => x.GetValueAsync<int>(It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>(), planSettings.DefaultMaxPlansPerSubscription))
                .Returns(Task.FromResult(planSettings.DefaultMaxPlansPerSubscription));

            planSettings.Init(mockSystemConfiguration.Object);
        }

        public static TheoryData<IEnumerable<VsoPlan>> ListPlansTests = new TheoryData<IEnumerable<VsoPlan>>
        {
            { new VsoPlan[] { MockPlan() } },
            { new VsoPlan[0] },
        };

        [Theory]
        [MemberData(nameof(ListPlansTests))]
        public async Task GetPlansWithCapacityWarning(IEnumerable<VsoPlan> plans)
        {
            var mockPlanManager = new Mock<IPlanManager>();

            mockPlanManager
                .Setup(obj => obj.ListAsync(
                    It.IsAny<UserIdSet>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IDiagnosticsLogger>(),
                    false))
                .Returns(Task.FromResult(plans));

            var logger = new Mock<IDiagnosticsLogger>().Object;
            var controller = CreateTestPlansController(mockPlanManager.Object);

            var result = await controller.ListByOwnerAsync(logger);
            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);

            var resultArray = okResult.Value as PlanResult[];
            Assert.NotNull(resultArray);

            Assert.Equal(plans.Count(), resultArray.Length);

            foreach (var expectedPlan in plans)
            {
                Assert.Contains(resultArray, (planResult) =>
                    planResult.Subscription == expectedPlan.Plan.Subscription &&
                    planResult.ResourceGroup == expectedPlan.Plan.ResourceGroup &&
                    planResult.Name == expectedPlan.Plan.Name);
            }
        }

        private PlansController CreateTestPlansController(IPlanManager planManager, ICurrentUserProvider currentUserProvider = null)
        {
            currentUserProvider ??= MockCurrentUserProvider();

            var controller = new PlansController(
                planManager,
                currentUserProvider);

            var httpContext = MockHttpContext.Create();
            var logger = new Mock<IDiagnosticsLogger>().Object;
            httpContext.SetLogger(logger);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
            };

            return controller;
        }

        private ICurrentUserProvider MockCurrentUserProvider(Dictionary<string, object> programs = null)
        {
            var moq = new Mock<ICurrentUserProvider>();
            moq
                .Setup(obj => obj.CurrentUserIdSet)
                .Returns(new UserIdSet("mock-profile-id"));
            moq
                .Setup(obj => obj.BearerToken)
                .Returns("mock-bearer-token");
            moq
                .Setup(obj => obj.GetProfileAsync())
                .Returns(() =>
                {
                    return Task.FromResult (new Profile
                    {
                        ProviderId = "mock-provider-id",
                        Programs = programs
                    });
                });

            return moq.Object;
        }

        private static VsoPlan MockPlan(string planName = "Test")
        {
            return new VsoPlan
            {
                Plan = new VsoPlanInfo
                {
                    Name = planName,
                    ResourceGroup = "myRG",
                    Subscription = Guid.NewGuid().ToString(),
                    Location = AzureLocation.WestUs2
                },
                UserId = "TestUser",
            };
        }
    }
}
