using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Test
{
    public class PlanControllerTest
    {
        private readonly IPlanRepository accountRepository;
        private readonly PlanManager accountManager;
        private readonly IDiagnosticsLoggerFactory loggerFactory;
        private readonly IDiagnosticsLogger logger; 

        public PlanControllerTest()
        {
            loggerFactory = new DefaultLoggerFactory();
            logger = loggerFactory.New();

            var planSettings = new PlanManagerSettings() { DefaultMaxPlansPerSubscription = 20 };

            var mockSystemConfiguration = new Mock<ISystemConfiguration>();
            mockSystemConfiguration
                .Setup(x => x.GetValueAsync<int>(It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>(), planSettings.DefaultMaxPlansPerSubscription))
                .Returns(Task.FromResult(planSettings.DefaultMaxPlansPerSubscription));

            planSettings.Init(mockSystemConfiguration.Object);

            accountRepository = new MockPlanRepository();
            accountManager = new PlanManager(accountRepository, planSettings);
        }

        public static TheoryData<IEnumerable<VsoPlan>, bool, Type> CapacityWarningTests = new TheoryData<IEnumerable<VsoPlan>, bool, Type>
        {
            { new VsoPlan[] { MockPlan() }, true, typeof(OkObjectResult) },     // Existing user, whitelisted
            { new VsoPlan[] { MockPlan() }, false, typeof(OkObjectResult) },    // Existing user, not whitelisted
            { new VsoPlan[0], true, typeof(OkObjectResult) },                   // New user, whitelisted
            { new VsoPlan[0], false, typeof(StatusCodeResult) },                // New user, not whitelisted
        };

        [Theory]
        [MemberData(nameof(CapacityWarningTests))]
        public async Task GetPlansWithCapacityWarning(IEnumerable<VsoPlan> plans, bool isWhiteListed, Type expectedResult)
        {
            var mockPlanManager = new Mock<IPlanManager>();

            mockPlanManager
                .Setup(obj => obj.ListAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>(), false))
                .Returns(Task.FromResult(plans));

            mockPlanManager
                .Setup(obj => obj.IsPlanCreationAllowedForUserAsync(It.IsAny<UserProfile.Profile>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(isWhiteListed));

            var logger = new Mock<IDiagnosticsLogger>().Object;
            var controller = CreateTestPlansController(mockPlanManager.Object);

            var result = await controller.ListByOwnerAsync(logger);
            Assert.IsType(expectedResult, result);

            if (result is StatusCodeResult statusResult)
            {
                Assert.Equal(503, statusResult.StatusCode);
            }
        }

        private PlansController CreateTestPlansController(IPlanManager planManager, ICurrentUserProvider currentUserProvider = null)
        {
            currentUserProvider = currentUserProvider ?? MockCurrentUserProvider();

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
                .Setup(obj => obj.GetProfileId())
                .Returns("mock-profile-id");
            moq
                .Setup(obj => obj.GetBearerToken())
                .Returns("mock-bearer-token");
            moq
                .Setup(obj => obj.GetProfile())
                .Returns(() => 
                {
                    return new UserProfile.Profile
                    {
                        ProviderId = "mock-provider-id",
                        Programs = programs
                    };
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
