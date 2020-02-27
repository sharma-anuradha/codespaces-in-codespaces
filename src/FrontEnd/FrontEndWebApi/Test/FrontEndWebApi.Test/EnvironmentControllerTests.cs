using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Test
{
    public class EnvironmentControllerTests
    {
        private readonly IPlanRepository accountRepository;
        private readonly PlanManager accountManager;
        private readonly IDiagnosticsLoggerFactory loggerFactory;
        private readonly IDiagnosticsLogger logger;

        public EnvironmentControllerTests()
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
            accountManager = new PlanManager(accountRepository, planSettings, MockUtil.MockSkuCatalog());
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

            var actionResult = await environmentController.GetAsync(expectedEnvId, logger);
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
            var mockEnv = MockUtil.MockCloudEnvironment();

            var mockEnvManager = new Mock<IEnvironmentManager>();
            mockEnvManager
                .Setup(x => x.ListAsync(It.IsAny<IDiagnosticsLogger>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserIdSet>()))
                .ReturnsAsync(new[] { mockEnv })
                .Callback((IDiagnosticsLogger logger, string planId, string name, UserIdSet userIdSet) =>
                {
                    Assert.Equal(expectedPlanId, planId);
                });

            var mockHttpContext = MockHttpContext.Create();
            mockHttpContext.SetPlan(tokenPlan);

            var environmentController = CreateTestEnvironmentsController(
                environmentManager: mockEnvManager.Object,
                httpContext: mockHttpContext);

            var actionResult = await environmentController.ListAsync(name: null, planId: queryParamPlan, logger: logger);
            Assert.IsType(expectedResultType, actionResult);
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
                Type = CloudEnvironmentType.CloudEnvironment.ToString(),
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
                Type = CloudEnvironmentType.CloudEnvironment.ToString(),
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
            var mockEnv = MockUtil.MockCloudEnvironment();

            var expectedRequestUri = "https://testhost/api/v1/environments/";
            var expectedEnvId = mockEnv.Id;

            var mockHttpContext = MockUtil.MockHttpContextFromUri(new Uri($"{expectedRequestUri}{expectedEnvId}"));

            var mockServiceUriBuilder = MockUtil.MockServiceUriBuilder(expectedRequestUri);

            var mockEnvManager = MockUtil.MockEnvironmentManager(mockEnv);

            var environmentController = CreateTestEnvironmentsController(
                environmentManager: mockEnvManager,
                httpContext: mockHttpContext,
                serviceUriBuilder: mockServiceUriBuilder);

            var actionResult = await environmentController.ResumeAsync(expectedEnvId, logger);
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
                    { ProfileExtensions.VisualStudioOnlineWidowsSkuPreviewUserProgram, true },
                },
                "test@microsoft.com");

            var environmentController = CreateTestEnvironmentsController(skuCatalog: skuCatalog, currentUserProvider: currentUser);

            // Create each enabled sku in west us 2
            foreach (var item in skuCatalog.EnabledInternalHardware())
            {
                var skuName = item.Key;
                var body = await CreateBodyAsync(skuName);
                var actionResult = await environmentController.CreateAsync(body, logger);
                Assert.IsType<CreatedResult>(actionResult);
            }
        }

        [Theory]
        [MemberData(nameof(Environments))]
        public async Task EnvironmentController_CloudEnvironmentAsync_SkuCatalog_BadRequest(string environment)
        {
            var skuCatalog = LoadSkuCatalog(environment);
            var environmentController = CreateTestEnvironmentsController(skuCatalog: skuCatalog);

            // Create each enabled sku in west us 2
            foreach (var item in skuCatalog.EnabledInternalHardware().Where((sku) => sku.Value.ComputeOS == ComputeOS.Windows))
            {
                var skuName = item.Key;
                var body = await CreateBodyAsync(skuName);
                var actionResult = await environmentController.CreateAsync(body, logger);
                Assert.IsType<BadRequestObjectResult>(actionResult);
            }
        }

        [Fact]
        public async Task EnvironmentController_CloudEnvironmentAsync_RedirectResult()
        {
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
        }

        [Fact]
        public async Task EnvironmentController_CloudEnvironmentAsync_BadResult()
        {
            var skuCatalog = LoadSkuCatalog("prod-rel");

            var currentUser = MockUtil.MockCurrentUserProvider(new Dictionary<string, object>
            {
                { ProfileExtensions.VisualStudioOnlineWidowsSkuPreviewUserProgram, true },
            });

            var environmentController = CreateTestEnvironmentsController(skuCatalog: skuCatalog, currentUserProvider: currentUser);

            var skuName = skuCatalog.CloudEnvironmentSkus.Keys.FirstOrDefault();

            // Not supported location
            var body = await CreateBodyAsync(skuName);
            var actionResult = await environmentController.CreateAsync(body, logger);
            Assert.IsType<BadRequestObjectResult>(actionResult);

            // Not supported SKU
            body = await CreateBodyAsync("bad-sku");
            actionResult = await environmentController.CreateAsync(body, logger);
            Assert.IsType<BadRequestObjectResult>(actionResult);
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
                            MockUtil.MockSku("MockSkuName", "MockSku", ComputeOS.Linux, 0, 0, new string[0]),
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
                .Setup(x => x.GetAndStateRefreshAsync(
                    It.IsAny<string>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(mockEnvironment))
                .Callback((string id, IDiagnosticsLogger log) =>
                    Assert.Equal(mockEnvironment.Id, id));

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

            var updateSettingsReponse = CloudEnvironmentSettingsUpdateResult.Success(mockEnvironment);

            var mockEnvironmentManager = new Mock<IEnvironmentManager>();

            mockEnvironmentManager
                .Setup(x => x.GetAndStateRefreshAsync(
                    It.IsAny<string>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(mockEnvironment))
                .Callback((string id, IDiagnosticsLogger log) =>
                    Assert.Equal(mockEnvironment.Id, id));

            mockEnvironmentManager
                .Setup(x => x.UpdateSettingsAsync(
                    It.IsAny<CloudEnvironment>(),
                    It.IsAny<CloudEnvironmentUpdate>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(updateSettingsReponse))
                .Callback((CloudEnvironment env, CloudEnvironmentUpdate update, IDiagnosticsLogger log) =>
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
            var updateSettingsReponse = CloudEnvironmentSettingsUpdateResult.Error(errorCodes);

            var mockEnvironmentManager = new Mock<IEnvironmentManager>();

            mockEnvironmentManager
                .Setup(x => x.GetAndStateRefreshAsync(
                    It.IsAny<string>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(mockEnvironment))
                .Callback((string id, IDiagnosticsLogger log) =>
                    Assert.Equal(mockEnvironment.Id, id));

            mockEnvironmentManager
                .Setup(x => x.UpdateSettingsAsync(
                    It.IsAny<CloudEnvironment>(),
                    It.IsAny<CloudEnvironmentUpdate>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(updateSettingsReponse))
                .Callback((CloudEnvironment env, CloudEnvironmentUpdate update, IDiagnosticsLogger log) =>
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

        private async Task<CreateCloudEnvironmentBody> CreateBodyAsync(string skuName)
        {
            return new CreateCloudEnvironmentBody
            {
                FriendlyName = "test-environment",
                PlanId = (await MockUtil.GeneratePlan()).Plan.ResourceId,
                AutoShutdownDelayMinutes = 5,
                Type = CloudEnvironmentType.CloudEnvironment.ToString(),
                SkuName = skuName,
            };
        }

        internal static EnvironmentsController CreateTestEnvironmentsController(
            ISkuCatalog skuCatalog = null, 
            ICurrentUserProvider currentUserProvider = null,
            IEnvironmentManager environmentManager = null,
            IPlanManager planManager = null,
            HttpContext httpContext = null,
            IServiceUriBuilder serviceUriBuilder = null)
        {
            environmentManager ??= MockUtil.MockEnvironmentManager();
            planManager ??= MockUtil.MockPlanManager(() => MockUtil.GeneratePlan());
            currentUserProvider ??= MockUtil.MockCurrentUserProvider();
            var controlPlaneInfo = MockUtil.MockControlPlaneInfo();
            var currentLocationProvider = MockUtil.MockCurrentLocationProvider();
            skuCatalog ??= MockUtil.MockSkuCatalog();
            var mapper = MockUtil.MockMapper();
            serviceUriBuilder ??= MockUtil.MockServiceUriBuilder();
            var settings = new FrontEndAppSettings
            {
                VSLiveShareApiEndpoint = MockUtil.MockServiceUri,
            };

            var environmentController = new EnvironmentsController(
                environmentManager,
                planManager,
                currentUserProvider,
                controlPlaneInfo,
                currentLocationProvider,
                skuCatalog,
                mapper,
                serviceUriBuilder,
                settings);

            httpContext ??= MockHttpContext.Create();
            var logger = new Mock<IDiagnosticsLogger>().Object;
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
