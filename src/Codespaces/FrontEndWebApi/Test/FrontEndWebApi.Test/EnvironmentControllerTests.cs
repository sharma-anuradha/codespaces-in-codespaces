using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repository.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Test
{
    public class EnvironmentControllerTests
    {
        private readonly IPlanRepository accountRepository;
        private readonly PlanManager accountManager;
        private readonly ISubscriptionManager subscriptionManager;
        private readonly IDiagnosticsLoggerFactory loggerFactory;
        private readonly IDiagnosticsLogger logger;

        public EnvironmentControllerTests()
        {
            loggerFactory = new DefaultLoggerFactory();
            logger = loggerFactory.New();

            var planSettings = new PlanManagerSettings() { DefaultMaxPlansPerSubscription = 20 };
            var mockSkuUtils = new Mock<ISkuUtils>();
            var mockSystemConfiguration = new Mock<ISystemConfiguration>();
            var currentLocationProvider = new Mock<ICurrentLocationProvider>().Object;

            mockSystemConfiguration
                .Setup(x => x.GetValueAsync<int>(It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>(), planSettings.DefaultMaxPlansPerSubscription))
                .Returns(Task.FromResult(planSettings.DefaultMaxPlansPerSubscription));

            planSettings.Init(mockSystemConfiguration.Object);

            accountRepository = new MockPlanRepository();
            subscriptionManager = new MockSubscriptionManager();
            accountManager = new PlanManager(accountRepository, planSettings, MockUtil.MockSkuCatalog(), currentLocationProvider);
        }

        [Fact]
        public void EnvironmentController_Constructor()
        {
            var environmentController = CreateTestEnvironmentsController();
            Assert.NotNull(environmentController);
        }

        [Fact]
        public async Task EnvironmentController_GetAsync()
        {
            var mockEnv = MockUtil.MockCloudEnvironment();
            var expectedEnvId = mockEnv.Id;

            var mockEnvManager = MockUtil.MockEnvironmentManager(mockEnv);
            var environmentController = CreateTestEnvironmentsController(
                environmentManager: mockEnvManager);

            var actionResult = await environmentController.GetAsync(Guid.Parse(expectedEnvId), logger);
            Assert.IsType<OkObjectResult>(actionResult);

            Assert.IsType<CloudEnvironmentResult>(((ObjectResult)actionResult).Value);
            var environment = (CloudEnvironmentResult)((ObjectResult)actionResult).Value;
            Assert.Equal(expectedEnvId, environment.Id);
            Assert.NotNull(environment.Connection);
            Assert.Equal(MockUtil.MockServiceUri, environment.Connection.ConnectionServiceUri);
        }

        public static TheoryData<string, string, string, Type> PlanAuthPlanIds = new TheoryData<string, string, string, Type>
        {
            { null,    null,   null,    typeof(OkObjectResult) }, // Not using plan access token => always OK
            { null,    "Plan", "Plan",  typeof(OkObjectResult) }, // Not using plan access token and specifying plan => OK, but should query the plan

            { "Token", null,   "Token", typeof(OkObjectResult) }, // Using plan access token => OK, but query should infer the plan
            { "Token", "Plan", "Plan",  typeof(ForbidResult) },   // Using plan access token and specifying different plan => Never OK
        };

        [Theory]
        [MemberData(nameof(PlanAuthPlanIds))]
        public async Task EnvironmentController_ListAsync_PlanAuth(string tokenPlan, string queryParamPlan, string expectedPlanId, Type expectedResultType)
        {
            // TODO: elpadann - move to EnvironmentListAction tests, as the authorization is no longer done in controller.
            _ = tokenPlan;
            _ = queryParamPlan;
            _ = expectedPlanId;
            _ = expectedResultType;
            await Task.CompletedTask;

            /*
            var mockEnv = MockUtil.MockCloudEnvironment();

            var mockEnvManager = new Mock<IEnvironmentManager>();
            mockEnvManager
                .Setup(x => x.ListAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserIdSet>(), It.IsAny<bool>(), It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync(new[] { mockEnv })
                .Callback((IDiagnosticsLogger logger, string planId, string name, UserIdSet userIdSet, bool deleted) =>
                {
                    Assert.Equal(expectedPlanId, planId);
                });

            var mockHttpContext = MockHttpContext.Create();
            var mockIdentity = new VsoClaimsIdentity(tokenPlan, null, null, new ClaimsIdentity());
            var mockCurrentUserProvider = MockUtil.MockCurrentUserProvider(identity: mockIdentity);

            var environmentController = CreateTestEnvironmentsController(
                environmentManager: mockEnvManager.Object,
                httpContext: mockHttpContext,
                currentUserProvider: mockCurrentUserProvider);

            var actionResult = await environmentController.ListAsync(name: null, planId: queryParamPlan, logger: logger);
            Assert.IsType(expectedResultType, actionResult);
            */
        }

        [Fact]
        public async Task EnvironmentController_CloudEnvironmentAsync()
        {
            var plan = await MockUtil.GeneratePlan();

            var body = new CreateCloudEnvironmentBody
            {
                FriendlyName = "test-environment",
                PlanId = plan.Plan.ResourceId,
                AutoShutdownDelayMinutes = 5,
                Type = EnvironmentType.CloudEnvironment.ToString(),
                SkuName = "testSkuName",
            };

            var environmentController = CreateTestEnvironmentsController();

            var actionResult = await environmentController.CreateAsync(body, logger);
            Assert.IsType<CreatedResult>(actionResult);

            Assert.IsType<CloudEnvironmentResult>(((ObjectResult)actionResult).Value);
            var environment = (CloudEnvironmentResult)((ObjectResult)actionResult).Value;
            Assert.NotNull(environment.Connection);
            Assert.Equal(MockUtil.MockServiceUri, environment.Connection.ConnectionServiceUri);
            Assert.Equal(plan.Plan.Location.ToString(), environment.Location);
        }

        public static TheoryData<Uri> CreateEnvironmentUrls = new TheoryData<Uri>
        {
            new Uri("https://testhost/api/v1/environments"),
            new Uri("https://testhost/api/v1/environments/"),
        };

        [Theory]
        [MemberData(nameof(CreateEnvironmentUrls))]
        public async Task EnvironmentController_CloudEnvironmentAsync_StartEnvironmentUrls(Uri uri)
        {
            var body = new CreateCloudEnvironmentBody
            {
                FriendlyName = "test-environment",
                PlanId = (await MockUtil.GeneratePlan()).Plan.ResourceId,
                AutoShutdownDelayMinutes = 5,
                Type = EnvironmentType.CloudEnvironment.ToString(),
                SkuName = "testSkuName",
            };

            var mockHttpContext = MockUtil.MockHttpContextFromUri(uri);

            var mockServiceUriBuilder = MockUtil.MockServiceUriBuilder(uri.ToString());

            var environmentController = CreateTestEnvironmentsController(
                httpContext: mockHttpContext,
                serviceUriBuilder: mockServiceUriBuilder);

            var actionResult = await environmentController.CreateAsync(body, logger);
            Assert.IsType<CreatedResult>(actionResult);
        }

        [Fact]
        public async Task EnvironmentController_StartAsync()
        {
            var planId = $"/subscriptions/{Guid.NewGuid()}/resourceGroups/cloudenvironment/providers/Microsoft.VSOnline/plans/my-good-plan";
            var mockEnv = MockUtil.MockCloudEnvironment(planId: planId);

            var expectedRequestUri = "https://testhost/api/v1/environments/";
            var expectedEnvId = mockEnv.Id;

            var mockHttpContext = MockUtil.MockHttpContextFromUri(new Uri($"{expectedRequestUri}{expectedEnvId}"));

            var mockServiceUriBuilder = MockUtil.MockServiceUriBuilder(expectedRequestUri);

            var mockEnvManager = MockUtil.MockEnvironmentManager(mockEnv);

            var environmentController = CreateTestEnvironmentsController(
                environmentManager: mockEnvManager,
                httpContext: mockHttpContext,
                serviceUriBuilder: mockServiceUriBuilder);

            var actionResult = await environmentController.ResumeAsync(Guid.Parse(expectedEnvId), logger);
            Assert.IsType<OkObjectResult>(actionResult);
        }

        public static TheoryData<string> Environments = new TheoryData<string>
        {
            "dev-ci",
            "ppe-rel",
            "prod-rel",
        };

        [Theory]
        [MemberData(nameof(Environments))]
        public async Task EnvironmentController_CloudEnvironmentAsync_SkuCatalog(string environment)
        {
            var skuCatalog = LoadSkuCatalog(environment);

            var currentUser = MockUtil.MockCurrentUserProvider(
                new Dictionary<string, object>
                {
                    { ProfileExtensions.VisualStudioOnlineWindowsSkuPreviewUserProgram, true },
                },
                "test@microsoft.com");

            var environmentController = CreateTestEnvironmentsController(skuCatalog: skuCatalog, currentUserProvider: currentUser);

            // Create each enabled sku in west us 2
            foreach (var item in skuCatalog.EnabledInternalHardware())
            {
                var skuName = item.Key;
                var body = await CreateBodyAsync(skuName);
                var actionResult = await environmentController.CreateAsync(body, logger);
                Assert.NotNull(actionResult);
                Assert.IsType<CreatedResult>(actionResult);
            }
        }

        [Fact]
        public async Task EnvironmentController_CloudEnvironmentAsync_RedirectResult()
        {
            // TODO: elpadann - move this test to manager layer, as location check is no longer done at controller.
            await Task.CompletedTask;

            /*
            var skuCatalog = LoadSkuCatalog("prod-rel");

            var planManager = MockUtil.MockPlanManager(() => MockUtil.GeneratePlan(location: AzureLocation.EastUs));

            var environmentController = CreateTestEnvironmentsController(
                skuCatalog: skuCatalog,
                planManager: planManager);

            var skuName = skuCatalog.CloudEnvironmentSkus.Keys.FirstOrDefault();

            // Redirect location
            var body = await CreateBodyAsync(skuName);
            var actionResult = await environmentController.CreateAsync(body, logger);
            Assert.IsType<RedirectResult>(actionResult);
            */
        }

        [Fact]
        public async Task EnvironmentController_CloudEnvironmentAsync_BadResult()
        {
            // TODO: elpadann - Move this test to the unit tests for EnvironmentCreateAction, since Sku validation is no longer in the controller.
            await Task.CompletedTask;

            /*
            var skuCatalog = LoadSkuCatalog("prod-rel");

            var currentUser = MockUtil.MockCurrentUserProvider(
                new Dictionary<string, object>
            {
                { ProfileExtensions.VisualStudioOnlineWindowsSkuPreviewUserProgram, true },
            });

            var environmentController = CreateTestEnvironmentsController(
                skuCatalog: skuCatalog,
                currentUserProvider: currentUser);

            // Not supported SKU
            var body = await CreateBodyAsync("bad-sku");
            var actionResult = await environmentController.CreateAsync(body, logger);
            Assert.IsType<BadRequestObjectResult>(actionResult);
            */

        }

        public static TheoryData<CloudEnvironmentAvailableSettingsUpdates, CloudEnvironmentAvailableUpdatesResult> SettingsUpdates =
            new TheoryData<CloudEnvironmentAvailableSettingsUpdates, CloudEnvironmentAvailableUpdatesResult>
            {
                {
                    new CloudEnvironmentAvailableSettingsUpdates
                    {
                        AllowedAutoShutdownDelayMinutes = new int[0],
                        AllowedSkus = new ICloudEnvironmentSku[0],
                    },
                    new CloudEnvironmentAvailableUpdatesResult
                    {
                        AllowedAutoShutdownDelayMinutes = new int[0],
                        AllowedSkus = new SkuInfoResult[0],
                    }
                },
                {
                    new CloudEnvironmentAvailableSettingsUpdates
                    {
                        AllowedAutoShutdownDelayMinutes = new[] { 0, 5, 30, 120 },
                        AllowedSkus = new ICloudEnvironmentSku[0],
                    },
                    new CloudEnvironmentAvailableUpdatesResult
                    {
                        AllowedAutoShutdownDelayMinutes = new[] { 0, 5, 30, 120 },
                        AllowedSkus = new SkuInfoResult[0],
                    }
                },
                {
                    new CloudEnvironmentAvailableSettingsUpdates
                    {
                        AllowedAutoShutdownDelayMinutes = new int[0],
                        AllowedSkus = new ICloudEnvironmentSku[]
                        {
                            MockUtil.MockSku("MockSkuName", SkuTier.Standard, "MockSku", ComputeOS.Linux, 0, 0, new string[0], new string[0], 1),
                        },
                    },
                    new CloudEnvironmentAvailableUpdatesResult
                    {
                        AllowedAutoShutdownDelayMinutes = new int[0],
                        AllowedSkus = new SkuInfoResult[]
                        {
                            new SkuInfoResult { Name = "MockSkuName" },
                        },
                    }
                },
            };

        [Theory]
        [MemberData(nameof(SettingsUpdates))]
        public async Task EnvironmentController_GetAvailableUpdates(
            CloudEnvironmentAvailableSettingsUpdates allowedUpdates,
            CloudEnvironmentAvailableUpdatesResult expectedResponse)
        {
            var mockEnvironment = MockUtil.MockCloudEnvironment();

            var mockEnvironmentManager = new Mock<IEnvironmentManager>();

            mockEnvironmentManager
                .Setup(x => x.GetAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(mockEnvironment))
                .Callback((Guid id, IDiagnosticsLogger log) =>
                    Assert.Equal(mockEnvironment.Id, id.ToString()));

            mockEnvironmentManager
                .Setup(x => x.GetAvailableSettingsUpdatesAsync(
                    It.IsAny<CloudEnvironment>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(allowedUpdates));

            var environmentController = CreateTestEnvironmentsController(environmentManager: mockEnvironmentManager.Object);

            var actionResult = await environmentController.GetAvailableSettingsUpdatesAsync(mockEnvironment.Id, logger);
            Assert.IsType<OkObjectResult>(actionResult);

            var result = (actionResult as OkObjectResult).Value as CloudEnvironmentAvailableUpdatesResult;

            Assert.NotNull(result);
            Assert.Equal(expectedResponse.AllowedAutoShutdownDelayMinutes, result.AllowedAutoShutdownDelayMinutes);
            Assert.Equal(expectedResponse.AllowedSkus.Length, result.AllowedSkus.Length);

            for (var i = 0; i < expectedResponse.AllowedSkus.Length; ++i)
            {
                var expectedSku = expectedResponse.AllowedSkus[i];
                var actual = result.AllowedSkus[i];

                Assert.Equal(expectedSku.Name, actual.Name);
            }
        }

        [Fact]
        public async Task EnvironmentController_UpdateSettingsAsync()
        {
            var mockEnvironment = MockUtil.MockCloudEnvironment();

            var updateRequest = new UpdateCloudEnvironmentBody
            {
                SkuName = "MockSku",
                AutoShutdownDelayMinutes = 123,
            };

            var updateSettingsReponse = CloudEnvironmentUpdateResult.Success(mockEnvironment);

            var mockEnvironmentManager = new Mock<IEnvironmentManager>();

            mockEnvironmentManager
                .Setup(x => x.GetAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(mockEnvironment))
                .Callback((Guid id, IDiagnosticsLogger log) =>
                    Assert.Equal(mockEnvironment.Id, id.ToString()));

            mockEnvironmentManager
                .Setup(x => x.UpdateSettingsAsync(
                    It.IsAny<CloudEnvironment>(),
                    It.IsAny<CloudEnvironmentUpdate>(),
                    It.IsAny<Subscription>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(updateSettingsReponse))
                .Callback((CloudEnvironment env, CloudEnvironmentUpdate update, Subscription sub, IDiagnosticsLogger log) =>
                {
                    Assert.Equal(updateRequest.SkuName, update.SkuName);
                    Assert.Equal(updateRequest.AutoShutdownDelayMinutes, update.AutoShutdownDelayMinutes);
                });

            var environmentController = CreateTestEnvironmentsController(environmentManager: mockEnvironmentManager.Object);

            var actionResult = await environmentController.UpdateSettingsAsync(mockEnvironment.Id, updateRequest, logger);
            Assert.IsType<OkObjectResult>(actionResult);

            var result = (actionResult as OkObjectResult).Value as CloudEnvironmentResult;

            Assert.NotNull(result);
            Assert.Equal(mockEnvironment.Id, result.Id);
        }

        [Fact]
        public async Task EnvironmentController_UpdateSettingsAsync_BadRequest()
        {
            var mockEnvironment = MockUtil.MockCloudEnvironment();

            var updateRequest = new UpdateCloudEnvironmentBody
            {
                SkuName = "MockSku",
                AutoShutdownDelayMinutes = 123,
            };

            var errorCodes = new List<MessageCodes> { MessageCodes.EnvironmentNotShutdown, MessageCodes.RequestedSkuIsInvalid, };
            var updateSettingsReponse = CloudEnvironmentUpdateResult.Error(errorCodes);

            var mockEnvironmentManager = new Mock<IEnvironmentManager>();

            mockEnvironmentManager
                .Setup(x => x.GetAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(mockEnvironment))
                .Callback((Guid id, IDiagnosticsLogger log) =>
                    Assert.Equal(mockEnvironment.Id, id.ToString()));

            mockEnvironmentManager
                .Setup(x => x.UpdateSettingsAsync(
                    It.IsAny<CloudEnvironment>(),
                    It.IsAny<CloudEnvironmentUpdate>(),
                    It.IsAny<Subscription>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(updateSettingsReponse))
                .Callback((CloudEnvironment env, CloudEnvironmentUpdate update, Subscription sub, IDiagnosticsLogger log) =>
                {
                    Assert.Equal(updateRequest.SkuName, update.SkuName);
                    Assert.Equal(updateRequest.AutoShutdownDelayMinutes, update.AutoShutdownDelayMinutes);
                });

            var environmentController = CreateTestEnvironmentsController(environmentManager: mockEnvironmentManager.Object);

            var actionResult = await environmentController.UpdateSettingsAsync(mockEnvironment.Id, updateRequest, logger);
            Assert.IsType<BadRequestObjectResult>(actionResult);

            var result = (actionResult as BadRequestObjectResult).Value as List<MessageCodes>;

            Assert.NotNull(result);
            Assert.Equal(errorCodes.Count, result.Count);

            for (var i = 0; i < errorCodes.Count; ++i)
            {
                var expectedCode = errorCodes[i];
                var actualCode = result[i];

                Assert.Equal(expectedCode, actualCode);
            }
        }

        [Fact]
        public async Task EnvironmentController_UpdatePlanAsync()
        {
            var plan1 = await MockUtil.GeneratePlan("plan1");
            var plan2 = await MockUtil.GeneratePlan("plan2");
            plan2.UserId = null;

            var mockEnvironment1 = MockUtil.MockCloudEnvironment(planId: plan1.Plan.ResourceId);
            var mockEnvironment2 = MockUtil.MockCloudEnvironment(planId: plan2.Plan.ResourceId);

            var mockAccessToken = "mock-token";
            var updateRequest = new UpdateCloudEnvironmentBody
            {
                PlanId = plan2.Plan.ResourceId,
                PlanAccessToken = mockAccessToken,
            };

            var updateSettingsReponse = CloudEnvironmentUpdateResult.Success(mockEnvironment2);

            var mockEnvironmentManager = new Mock<IEnvironmentManager>();

            mockEnvironmentManager
                .Setup(x => x.GetAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(mockEnvironment1))
                .Callback((Guid id, IDiagnosticsLogger log) =>
                    Assert.Equal(mockEnvironment1.Id, id.ToString()));

            mockEnvironmentManager
                .Setup(x => x.UpdateSettingsAsync(
                    It.IsAny<CloudEnvironment>(),
                    It.IsAny<CloudEnvironmentUpdate>(),
                    It.IsAny<Subscription>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(updateSettingsReponse))
                .Callback((CloudEnvironment env, CloudEnvironmentUpdate update, Subscription sub, IDiagnosticsLogger log) =>
                {
                    Assert.NotNull(update.Plan);
                });

            var mockPlanManager = MockUtil.MockPlanManager(() => Task.FromResult(plan2));

            // These claims are returned for the mock access token, to grant access to plan2.
            var planAccessClaims = new[]
            {
                new Claim(CustomClaims.PlanResourceId, plan2.Plan.ResourceId),
                new Claim(CustomClaims.Scope, PlanAccessTokenScopes.WriteEnvironments),
            };

            var mockTokenReader = new Mock<ICascadeTokenReader>();
            mockTokenReader
                .Setup((x) => x.ReadTokenPrincipal(It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(new ClaimsPrincipal(new ClaimsIdentity(planAccessClaims)))
                .Callback((string token, IDiagnosticsLogger logger) =>
                {
                    Assert.Equal(mockAccessToken, token);
                });

            var environmentController = CreateTestEnvironmentsController(
                environmentManager: mockEnvironmentManager.Object,
                planManager: mockPlanManager,
                accessTokenReader: mockTokenReader.Object);

            var actionResult = await environmentController.UpdateSettingsAsync(
                mockEnvironment1.Id, updateRequest, logger);
            Assert.IsType<OkObjectResult>(actionResult);

            var result = (actionResult as OkObjectResult).Value as CloudEnvironmentResult;

            Assert.NotNull(result);
            Assert.Equal(mockEnvironment1.Id, result.Id);
            Assert.Equal(plan2.Plan.ResourceId, result.PlanId);
        }

        [Fact]
        public async Task EnvironmentController_UpdatePlan_Forbidden()
        {
            var plan1 = await MockUtil.GeneratePlan("plan1");
            var plan2 = await MockUtil.GeneratePlan("plan2");
            plan2.UserId = null;

            var mockEnvironment1 = MockUtil.MockCloudEnvironment(planId: plan1.Plan.ResourceId);
            var mockEnvironment2 = MockUtil.MockCloudEnvironment(planId: plan2.Plan.ResourceId);

            var mockAccessToken = "mock-token";
            var updateRequest = new UpdateCloudEnvironmentBody
            {
                PlanId = plan2.Plan.ResourceId,
                PlanAccessToken = mockAccessToken,
            };

            var updateSettingsReponse = CloudEnvironmentUpdateResult.Success(mockEnvironment2);

            var mockEnvironmentManager = new Mock<IEnvironmentManager>();

            mockEnvironmentManager
                .Setup(x => x.GetAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(mockEnvironment1))
                .Callback((Guid id, IDiagnosticsLogger log) =>
                    Assert.Equal(mockEnvironment1.Id, id.ToString()));

            mockEnvironmentManager
                .Setup(x => x.UpdateSettingsAsync(
                    It.IsAny<CloudEnvironment>(),
                    It.IsAny<CloudEnvironmentUpdate>(),
                    It.IsAny<Subscription>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(updateSettingsReponse))
                .Callback((CloudEnvironment env, CloudEnvironmentUpdate update, Subscription sub, IDiagnosticsLogger log) =>
                {
                    Assert.NotNull(update.Plan);
                });

            var mockPlanManager = MockUtil.MockPlanManager(() => Task.FromResult(plan2));

            // The claim of plan1 ID does NOT grant access to plan2.
            var planAccessClaims = new[]
            {
                new Claim(CustomClaims.PlanResourceId, plan1.Plan.ResourceId),
                new Claim(CustomClaims.Scope, PlanAccessTokenScopes.WriteEnvironments),
            };

            var mockTokenReader = new Mock<ICascadeTokenReader>();
            mockTokenReader
                .Setup((x) => x.ReadTokenPrincipal(It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(new ClaimsPrincipal(new ClaimsIdentity(planAccessClaims)))
                .Callback((string token, IDiagnosticsLogger logger) =>
                {
                    Assert.Equal(mockAccessToken, token);
                });

            var environmentController = CreateTestEnvironmentsController(
                environmentManager: mockEnvironmentManager.Object,
                planManager: mockPlanManager,
                accessTokenReader: mockTokenReader.Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await environmentController.UpdateSettingsAsync(mockEnvironment1.Id, updateRequest, logger));
        }

        [Fact]
        public async Task EnvironmentController_UpdateRecentFoldersListAsync()
        {
            var mockEnvironment = MockUtil.MockCloudEnvironment();

            var updateRequest = new CloudEnvironmentFolderBody
            {
                RecentFolderPaths = new List<string> { "/home/vsonline/otherWorkspaceHi" }
            };

            var updateSettingsReponse = CloudEnvironmentUpdateResult.Success(mockEnvironment);

            var mockEnvironmentManager = new Mock<IEnvironmentManager>();

            mockEnvironmentManager
                .Setup(x => x.GetAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(mockEnvironment))
                .Callback((Guid id, IDiagnosticsLogger log) =>
                    Assert.Equal(mockEnvironment.Id, id.ToString()));

            mockEnvironmentManager
                .Setup(x => x.UpdateFoldersListAsync(
                    It.IsAny<CloudEnvironment>(),
                    It.IsAny<CloudEnvironmentFolderBody>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(updateSettingsReponse))
                .Callback((CloudEnvironment env, CloudEnvironmentFolderBody update, IDiagnosticsLogger log) =>
                {
                    Assert.Equal(updateRequest.RecentFolderPaths, update.RecentFolderPaths);
                });

            var environmentController = CreateTestEnvironmentsController(environmentManager: mockEnvironmentManager.Object);

            var actionResult = await environmentController.UpdateRecentFoldersListAsync(mockEnvironment.Id, updateRequest, logger);
            Assert.IsType<OkObjectResult>(actionResult);

            var result = (actionResult as OkObjectResult).Value as CloudEnvironmentResult;

            Assert.NotNull(result);
            Assert.Equal(mockEnvironment.Id, result.Id);
        }

        [Fact]
        public async Task EnvironmentController_UpdateRecentFoldersAsync_BadRequest()
        {
            var mockEnvironment = MockUtil.MockCloudEnvironment();

            var updateRequest = new CloudEnvironmentFolderBody
            {
                RecentFolderPaths = new List<string> { "/home/vsonline/otherWorkspaceHi" }
            };

            var errorCodes = new List<MessageCodes> { MessageCodes.TooManyRecentFolders, MessageCodes.FilePathIsInvalid, };
            var updateSettingsReponse = CloudEnvironmentUpdateResult.Error(errorCodes);

            var mockEnvironmentManager = new Mock<IEnvironmentManager>();

            mockEnvironmentManager
                .Setup(x => x.GetAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(mockEnvironment))
                .Callback((Guid id, IDiagnosticsLogger log) =>
                    Assert.Equal(mockEnvironment.Id, id.ToString()));

            mockEnvironmentManager
                .Setup(x => x.UpdateFoldersListAsync(
                    It.IsAny<CloudEnvironment>(),
                    It.IsAny<CloudEnvironmentFolderBody>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(updateSettingsReponse))
                .Callback((CloudEnvironment env, CloudEnvironmentFolderBody update, IDiagnosticsLogger log) =>
                {
                    Assert.Equal(updateRequest.RecentFolderPaths, update.RecentFolderPaths);
                });

            var environmentController = CreateTestEnvironmentsController(environmentManager: mockEnvironmentManager.Object);

            var actionResult = await environmentController.UpdateRecentFoldersListAsync(mockEnvironment.Id, updateRequest, logger);

            Assert.IsType<BadRequestObjectResult>(actionResult);

            var result = (actionResult as BadRequestObjectResult).Value as List<MessageCodes>;

            Assert.NotNull(result);
            Assert.Equal(errorCodes.Count, result.Count);

            for (var i = 0; i < errorCodes.Count; ++i)
            {
                var expectedCode = errorCodes[i];
                var actualCode = result[i];

                Assert.Equal(expectedCode, actualCode);
            }
        }

        [Fact]
        public void QuotaHeaderAttribute_ResponseContainsQuotaHeaders()
        {
            var computeUsage = 100;
            var computeQuota = 200;
            var subscription = Guid.NewGuid().ToString();
            var subscriptionState = SubscriptionStateEnum.Registered.ToString();
            var computeUsageText = "x-codespaces-core-usage";
            var computeQuotaText = "x-codespaces-core-limit";
            var results = new CloudEnvironmentResult
            {
                SubscriptionData = new HttpContracts.Subscriptions.SubscriptionData
                {
                    ComputeUsage = computeUsage,
                    ComputeQuota = computeQuota,
                    SubscriptionId = subscription,
                    SubscriptionState = subscriptionState
                }
            };
            var httpContextMock = new DefaultHttpContext();
            var loggerFactory = new DefaultLoggerFactory();
            var logger = loggerFactory.New();
            httpContextMock.SetLogger(logger);
            var actionContext = new ActionContext(
            httpContextMock,
            Mock.Of<RouteData>(),
            Mock.Of<ActionDescriptor>(),
            new ModelStateDictionary());

            var actionExecutingContext = new ActionExecutedContext(
                actionContext,
                new List<IFilterMetadata>(),
                Mock.Of<Controller>()
            )
            {
                Result = new OkObjectResult(results)
            };

            var quotaAttribute = new QuotaHeaderAttribute();
            quotaAttribute.OnActionExecuted(actionExecutingContext);

            var responseHttpContext = actionExecutingContext.HttpContext;
            responseHttpContext.Response.Headers.TryGetValue(computeUsageText, out var computeUsageValue);
            responseHttpContext.Response.Headers.TryGetValue(computeQuotaText, out var computeQuotaValue);
            
            Assert.Equal(computeUsage.ToString(), computeUsageValue);
            Assert.Equal(computeQuota.ToString(), computeQuotaValue);
        }

        private async Task<CreateCloudEnvironmentBody> CreateBodyAsync(string skuName)
        {
            return new CreateCloudEnvironmentBody
            {
                FriendlyName = "test-environment",
                PlanId = (await MockUtil.GeneratePlan()).Plan.ResourceId,
                AutoShutdownDelayMinutes = 5,
                Type = EnvironmentType.CloudEnvironment.ToString(),
                SkuName = skuName,
            };
        }

        internal static EnvironmentsController CreateTestEnvironmentsController(
            ISkuCatalog skuCatalog = null,
            ICurrentUserProvider currentUserProvider = null,
            IEnvironmentManager environmentManager = null,
            IPlanManager planManager = null,
            HttpContext httpContext = null,
            IServiceUriBuilder serviceUriBuilder = null,
            ISkuUtils skuUtils = null,
            ICascadeTokenReader accessTokenReader = null)
        {
            environmentManager ??= MockUtil.MockEnvironmentManager();
            planManager ??= MockUtil.MockPlanManager(() => MockUtil.GeneratePlan());
            currentUserProvider ??= MockUtil.MockCurrentUserProvider();
            var controlPlaneInfo = MockUtil.MockControlPlaneInfo();
            var currentLocationProvider = MockUtil.MockCurrentLocationProvider();
            skuCatalog ??= MockUtil.MockSkuCatalog();
            var mapper = MockUtil.MockMapper();
            serviceUriBuilder ??= MockUtil.MockServiceUriBuilder();
            skuUtils ??= MockUtil.MockSkuUtils(true);
            var metrics = MockUtil.MockMetricsManager();
            var subManager = MockUtil.MockSubscriptionManager();
            var environmentAccessManager = new EnvironmentAccessManager(currentUserProvider);
            var globalRepository = new MockGlobalCloudEnvironmentRepository();
            var regionalRepository = new MockRegionalCloudEnvironmentRepository();
            var environmentRepositoryManager = new CloudEnvironmentRepository(globalRepository, regionalRepository);
            var billingEventManager = new BillingEventManager(new MockBillingEventRepository(),
                                                                new MockBillingOverrideRepository());
            var workspaceManager = new WorkspaceManager(new MockClientWorkspaceRepository());
            var metricsLogger = new MockEnvironmentMetricsLogger();
            var environmentStateChangeManager = new Mock<IEnvironmentStateChangeManager>().Object;
            var environmentStateManager = new EnvironmentStateManager(workspaceManager, environmentRepositoryManager, billingEventManager, environmentStateChangeManager, metricsLogger);

            var environmentSettings = new EnvironmentManagerSettings();

            var mockSystemConfiguration = new Mock<ISystemConfiguration>();
            mockSystemConfiguration
                    .Setup(x => x.GetValueAsync(It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>(), It.IsAny<bool>()))
                    .Returns(Task.FromResult(true));
            mockSystemConfiguration
                    .Setup(x => x.GetValueAsync(
                        It.Is<string>(x => x == "featureflag:environment-workspace-status-to-normalize-enabled"),
                        It.IsAny<IDiagnosticsLogger>(),
                        It.IsAny<bool>()))
                    .Returns(Task.FromResult(false));

            environmentSettings.Init(mockSystemConfiguration.Object);

            var settings = new FrontEndAppSettings
            {
                VSLiveShareApiEndpoint = MockUtil.MockServiceUri,
                EnvironmentManagerSettings = environmentSettings
            };
            var tokenProvider = new Mock<ITokenProvider>();
            accessTokenReader ??= new Mock<ICascadeTokenReader>().Object;
            var gitHubApiClientProvider = new Mock<IGithubApiHttpClientProvider>().Object;
            var gitHubFixedPlansMapper = new GitHubFixedPlansMapper(currentLocationProvider, settings, gitHubApiClientProvider);

            var environmentController = new EnvironmentsController(
                environmentManager,
                planManager,
                currentUserProvider,
                controlPlaneInfo,
                currentLocationProvider,
                skuCatalog,
                mapper,
                serviceUriBuilder,
                settings,
                skuUtils,
                tokenProvider.Object,
                metrics,
                subManager,
                accessTokenReader,
                environmentAccessManager,
                environmentStateManager,
                gitHubFixedPlansMapper);
            var logger = new Mock<IDiagnosticsLogger>().Object;

            httpContext ??= MockHttpContext.Create();
            httpContext.SetLogger(logger);

            environmentController.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
            };

            return environmentController;
        }

        private ISkuCatalog LoadSkuCatalog(string environmentName)
        {
            var appSettings = LoadAppSettings(environmentName);
            var controlPlaneInfo = new Mock<IControlPlaneInfo>();
            controlPlaneInfo
                .Setup(obj => obj.EnvironmentResourceGroupName)
                .Returns("test-environment-rg");
            controlPlaneInfo
                .Setup(obj => obj.Stamp.DataPlaneLocations)
                .Returns(new List<AzureLocation>
                {
                    AzureLocation.WestUs2,
                });

            var subscriptionId = Guid.NewGuid().ToString();
            var controlPlaneAzureResourceAccessor = new Mock<IControlPlaneAzureResourceAccessor>();
            controlPlaneAzureResourceAccessor
                .Setup(obj => obj.GetCurrentSubscriptionIdAsync())
                .Returns(Task.FromResult(subscriptionId));

            var currentImageInfoProvider = new Mock<ICurrentImageInfoProvider>();
            currentImageInfoProvider
                .Setup(x => x.GetImageNameAsync(It.IsAny<ImageFamilyType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns((ImageFamilyType familyType, string family, string defaultName, IDiagnosticsLogger logger) => Task.FromResult(defaultName));
            currentImageInfoProvider
                .Setup(x => x.GetImageVersionAsync(It.IsAny<ImageFamilyType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns((ImageFamilyType familyType, string family, string defaultVersion, IDiagnosticsLogger logger) => Task.FromResult(defaultVersion));

            var skuCatalog = new SkuCatalog(
                appSettings.SkuCatalogSettings,
                controlPlaneInfo.Object,
                controlPlaneAzureResourceAccessor.Object,
                currentImageInfoProvider.Object);

            return skuCatalog;
        }

        public static AppSettingsBase LoadAppSettings(string environmentName, string overrideName = null)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.images.json", optional: false)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: false);

            if (!string.IsNullOrEmpty(overrideName))
            {
                builder.AddJsonFile($"appsettings.{overrideName}.json", optional: false);
            }

            var configuration = builder.Build();
            var appSettingsConfiguration = configuration.GetSection("AppSettings");
            var appSettings = appSettingsConfiguration.Get<AppSettingsBase>();
            return appSettings;
        }
    }
}
