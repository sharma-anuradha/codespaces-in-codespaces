using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Test
{
    public class SubscriptionsControllerTests
    {
        private const string MockSubscriptionId = "00000000-0000-0000-0000-000000000000";
        private const string MockResourceGroup = "MockResourceGroup";
        private const string MockPlanName = "MockPlanName";
        private const string ResourceProviderName = "Microsoft.VSOnline";
        private const string PlanResourceTypeName = "plans";

        public static TheoryData<(IEnumerable<VsoPlan> known, IEnumerable<VsoPlan> unknown, IEnumerable<VsoPlan> expected)> VerifyPlanListBySubscriptionAsyncCases =
            new TheoryData<(IEnumerable<VsoPlan> known, IEnumerable<VsoPlan> unknown, IEnumerable<VsoPlan> expected)>
            {
                {(
                    known: new List<VsoPlan>
                    {
                        MockVsoPlan(),
                    },
                    unknown: null,
                    expected: new List<VsoPlan>
                    {
                        MockVsoPlan(),
                    }
                )},
                {(
                    known: null,
                    unknown: new List<VsoPlan>
                    {
                        MockVsoPlan(),
                    },
                    expected: Enumerable.Empty<VsoPlan>()
                )},
                {(
                    known: new List<VsoPlan>
                    {
                        MockVsoPlan(MockSubscriptionId, MockResourceGroup, "known-plan"),
                    },
                    unknown: new List<VsoPlan>
                    {
                        MockVsoPlan(MockSubscriptionId, MockResourceGroup, "unknown-plan"),
                    },
                    expected: new List<VsoPlan>
                    {
                        MockVsoPlan(MockSubscriptionId, MockResourceGroup, "known-plan"),
                    }
                )},
                {(
                    known: new List<VsoPlan>
                    {
                        MockVsoPlan(subscription: "11111111-1111-1111-1111-111111111111"),
                    },
                    unknown: new List<VsoPlan>
                    {
                        MockVsoPlan(subscription: MockSubscriptionId),
                    },
                    expected: Enumerable.Empty<VsoPlan>()
                )},
            };

        [Theory]
        [MemberData(nameof(VerifyPlanListBySubscriptionAsyncCases))]
        public async Task VerifyPlanListBySubscriptionAsync((IEnumerable<VsoPlan> known, IEnumerable<VsoPlan> unknown, IEnumerable<VsoPlan> expected) test)
        {
            var (knownPlans, unknownPlans, expectedPlans) = test;

            knownPlans ??= Enumerable.Empty<VsoPlan>();
            unknownPlans ??= Enumerable.Empty<VsoPlan>();

            var planManager = MockPlanManager(knownPlans);

            var controller = CreateTestController(planManager: planManager);

            var resourceList = new PlanResourceList
            {
                Value = knownPlans.Select(ToPlanResource).Union(unknownPlans.Select(ToPlanResource)),
            };

            var result = await controller.PlanListBySubscriptionAsync(MockSubscriptionId, ResourceProviderName, PlanResourceTypeName, resourceList);

            var jsonResult = result as JsonResult;
            Assert.NotNull(jsonResult);

            var resultList = jsonResult.Value as PlanResourceList;
            Assert.NotNull(resultList);

            var actualResources = resultList.Value;
            Assert.NotNull(actualResources);

            var expectedResources = expectedPlans.Select(ToPlanResource);

            Assert.Equal(expectedResources.Count(), actualResources.Count());
            Assert.All(expectedResources, (expected) => actualResources.Any((actual) => actual.Id == expected.Id));
        }

        public static TheoryData<(IEnumerable<VsoPlan> known, IEnumerable<VsoPlan> unknown, IEnumerable<VsoPlan> expected)> VerifyPlanListByResourceGroupAsyncpCases =
            new TheoryData<(IEnumerable<VsoPlan> known, IEnumerable<VsoPlan> unknown, IEnumerable<VsoPlan> expected)>
            {
                {(
                    known: new List<VsoPlan>
                    {
                        MockVsoPlan(),
                    },
                    unknown: null,
                    expected: new List<VsoPlan>
                    {
                        MockVsoPlan(),
                    }
                )},
                {(
                    known: null,
                    unknown: new List<VsoPlan>
                    {
                        MockVsoPlan(),
                    },
                    expected: Enumerable.Empty<VsoPlan>()
                )},
                {(
                    known: new List<VsoPlan>
                    {
                        MockVsoPlan(MockSubscriptionId, MockResourceGroup, "known-plan"),
                    },
                    unknown: new List<VsoPlan>
                    {
                        MockVsoPlan(MockSubscriptionId, MockResourceGroup, "unknown-plan"),
                    },
                    expected: new List<VsoPlan>
                    {
                        MockVsoPlan(MockSubscriptionId, MockResourceGroup, "known-plan"),
                    }
                )},
                {(
                    known: new List<VsoPlan>
                    {
                        MockVsoPlan(resourceGroup: "bad-rg"),
                    },
                    unknown: new List<VsoPlan>
                    {
                        MockVsoPlan(resourceGroup: MockResourceGroup),
                    },
                    expected: Enumerable.Empty<VsoPlan>()
                )},
                {(
                    known: new List<VsoPlan>
                    {
                        MockVsoPlan(subscription: "11111111-1111-1111-1111-111111111111"),
                    },
                    unknown: new List<VsoPlan>
                    {
                        MockVsoPlan(subscription: MockSubscriptionId),
                    },
                    expected: Enumerable.Empty<VsoPlan>()
                )},
            };

        [Theory]
        [MemberData(nameof(VerifyPlanListByResourceGroupAsyncpCases))]
        public async Task VerifyPlanListByResourceGroupAsync((IEnumerable<VsoPlan> known, IEnumerable<VsoPlan> unknown, IEnumerable<VsoPlan> expected) test)
        {
            var (knownPlans, unknownPlans, expectedPlans) = test;

            knownPlans ??= Enumerable.Empty<VsoPlan>();
            unknownPlans ??= Enumerable.Empty<VsoPlan>();

            var planManager = MockPlanManager(knownPlans);

            var controller = CreateTestController(planManager: planManager);

            var resourceList = new PlanResourceList
            {
                Value = knownPlans.Select(ToPlanResource).Union(unknownPlans.Select(ToPlanResource)),
            };

            var result = await controller.PlanListByResourceGroupAsync(MockSubscriptionId, MockResourceGroup, ResourceProviderName, PlanResourceTypeName, resourceList);

            var jsonResult = result as JsonResult;
            Assert.NotNull(jsonResult);

            var resultList = jsonResult.Value as PlanResourceList;
            Assert.NotNull(resultList);

            var actualResources = resultList.Value;
            Assert.NotNull(actualResources);

            var expectedResources = expectedPlans.Select(ToPlanResource);

            Assert.Equal(expectedResources.Count(), actualResources.Count());
            Assert.All(expectedResources, (expected) => actualResources.Any((actual) => actual.Id == expected.Id));
        }

        [Fact]
        public async Task VerifyPlanGetAsync()
        {
            var plan = MockVsoPlan();
            var planManager = MockPlanManager(new[] { plan });

            var controller = CreateTestController(planManager: planManager);

            var result = await controller.PlanGetAsync(plan.Plan.Subscription, plan.Plan.ResourceGroup, ResourceProviderName, PlanResourceTypeName, plan.Plan.Name, ToPlanResource(plan));

            var jsonResult = result as JsonResult;
            Assert.NotNull(jsonResult);

            var resultResource = jsonResult.Value as PlanResource;
            Assert.NotNull(resultResource);
            Assert.Equal(plan.Plan.ResourceId, resultResource.Id);
        }

        [Fact]
        public async Task VerifyPlanGetAsync_NotFound()
        {
            var planManager = MockPlanManager();

            var controller = CreateTestController(planManager: planManager);

            var result = await controller.PlanGetAsync(MockSubscriptionId, MockResourceGroup, ResourceProviderName, PlanResourceTypeName, MockPlanName, ToPlanResource(MockVsoPlan()));

            var jsonResult = result as JsonResult;
            Assert.NotNull(jsonResult);

            Assert.Equal((int) HttpStatusCode.NotFound, jsonResult.StatusCode);
        }

        private static SubscriptionsController CreateTestController(
            IEnvironmentManager environmentManager = null,
            IPlanManager planManager = null,
            HttpContext httpContext = null)
        {
            environmentManager ??= MockUtil.MockEnvironmentManager();
            planManager ??= MockPlanManager();
            var mapper = MockUtil.MockMapper();
            var configuration = new Mock<ISystemConfiguration>();
            var tokenProvider = new Mock<ITokenProvider>();
            var currentUserProvider = new Mock<ICurrentUserProvider>();

            var controller = new SubscriptionsController(
                planManager,
                tokenProvider.Object,
                mapper,
                environmentManager,
                configuration.Object,
                currentUserProvider.Object);

            var logger = new Mock<IDiagnosticsLogger>();
            // This is called inside HttpContext.HttpScopeAsync
            logger.Setup(x => x.WithValues(It.IsAny<LogValueSet>())).Returns(logger.Object);

            httpContext ??= MockHttpContext.Create();
            httpContext.SetLogger(logger.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
            };

            return controller;
        }

        private static IPlanManager MockPlanManager(IEnumerable<VsoPlan> knownPlans = null)
        {
            var mock = new Mock<IPlanManager>();

            mock.Setup(x => x.GetAsync(It.IsAny<VsoPlanInfo>(), It.IsAny<IDiagnosticsLogger>(), It.IsAny<bool>()))
                .ReturnsAsync
                (
                    (VsoPlanInfo planInfo, IDiagnosticsLogger logger, bool includeDeleted) => 
                        knownPlans?.FirstOrDefault((plan) => plan.Plan.ResourceId == planInfo.ResourceId)
                );

            mock.Setup(x => x.ListAsync(It.IsAny<UserIdSet>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>(), It.IsAny<bool>()))
               .ReturnsAsync
               (
                   (UserIdSet user, string sub, string resGroup, string name, IDiagnosticsLogger logger, bool includeDeleted) =>
                       knownPlans?.Where
                       (
                           (plan) => 
                                (sub == null || plan.Plan.Subscription == sub) &&
                                (resGroup == null || plan.Plan.ResourceGroup == resGroup) &&
                                (name == null || plan.Plan.Name == name)
                       ) ?? Enumerable.Empty<VsoPlan>()
               );

            return mock.Object;
        }

        private static VsoPlan MockVsoPlan
        (
            string subscription = MockSubscriptionId,
            string resourceGroup = MockResourceGroup, 
            string name = MockPlanName
        )
        {
            return new VsoPlan
            {
                Plan = new VsoPlanInfo
                {
                    Subscription = subscription,
                    ResourceGroup = resourceGroup,
                    Name = name,
                },
            };
        }

        private static PlanResource ToPlanResource(VsoPlan vsoPlan)
        {
            return new PlanResource
            {
                Id = vsoPlan.Plan.ResourceId,
                Name = vsoPlan.Plan.Name,
            };
        }
    }
}
