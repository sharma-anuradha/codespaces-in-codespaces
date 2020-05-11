using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.RepairWorkflows;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repositories.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveshareAuthentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Microsoft.VsSaaS.Tokens;
using Moq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Test
{
    public class EnvironmentManagerTestsBase
    {
        public readonly MockCloudEnvironmentRepository environmentRepository;
        public readonly MockPlanRepository planRepository;
        public readonly MockBillingEventRepository billingEventRepository;
        public readonly MockClientWorkspaceRepository workspaceRepository;
        public readonly IBillingEventManager billingEventManager;
        public readonly MockClientAuthRepository authRepository;
        public readonly IResourceBrokerResourcesExtendedHttpContract resourceBroker;
        public readonly ITokenProvider tokenProvider;
        public readonly EnvironmentManager environmentManager;
        public readonly ISubscriptionManager subscriptionManager;
        private readonly ResourceSelectorFactory resourceSelector;
        public readonly IDiagnosticsLoggerFactory loggerFactory;
        public readonly IDiagnosticsLogger logger;
        public readonly ISkuCatalog skuCatalog;
        private readonly List<IEnvironmentRepairWorkflow> environmentRepairWorkflows;
        private readonly IResourceAllocationManager resourceAllocationManager;
        private readonly IWorkspaceManager workspaceManager;
        public readonly IEnvironmentMonitor environmentMonitor;
        public readonly IEnvironmentStateManager environmentStateManager;
        public const string testUserId = "test-user";
        public static readonly UserIdSet testUserIdSet = new UserIdSet(testUserId, testUserId, testUserId);
        public const string testAccessToken = "test-token";
        public readonly Uri testServiceUri = new Uri("http://localhost/");
        public const string testCallbackUriFormat = "http://localhost/{0}";

        public static readonly VsoPlanInfo testPlan = new VsoPlanInfo
        {
            Subscription = "00000000-0000-0000-0000-000000000000",
            ResourceGroup = "test-resourcegroup",
            Name = "test-plan",
            Location = AzureLocation.WestUs2
        };
        public static readonly VsoPlan testVsoPlan = new VsoPlan
        {
            Id = "11100000-0000-0000-0000-000000000000",
            Plan = testPlan,
        };

        public EnvironmentManagerTestsBase()
        {
            loggerFactory = new DefaultLoggerFactory();
            logger = loggerFactory.New();

            var defaultCount = 20;
            var planSettings = new PlanManagerSettings()
            {
                DefaultMaxPlansPerSubscription = defaultCount,
                DefaultAutoSuspendDelayMinutesOptions = new int[] { 0, 5, 30, 120 },
            };
            var environmentSettings = new EnvironmentManagerSettings() { DefaultMaxEnvironmentsPerPlan = defaultCount };

            var mockSystemConfiguration = new Mock<ISystemConfiguration>();
            mockSystemConfiguration
                .Setup(x => x.GetValueAsync<int>(It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>(), defaultCount))
                .Returns(Task.FromResult(defaultCount));

            planSettings.Init(mockSystemConfiguration.Object);
            environmentSettings.Init(mockSystemConfiguration.Object);

            this.environmentRepository = new MockCloudEnvironmentRepository();
            this.planRepository = new MockPlanRepository();
            this.billingEventRepository = new MockBillingEventRepository();
            this.billingEventManager = new BillingEventManager(this.billingEventRepository,
                                                                new MockBillingOverrideRepository());
            workspaceRepository = new MockClientWorkspaceRepository();
            tokenProvider = MockTokenProvider();
            resourceBroker = new MockResourceBrokerClient();
            this.environmentMonitor = new MockEnvironmentMonitor();
            var metricsLogger = new MockEnvironmentMetricsLogger();

            var skuMock = new Mock<ICloudEnvironmentSku>(MockBehavior.Strict);
            skuMock.Setup((s) => s.ComputeOS).Returns(ComputeOS.Linux);

            var skuDictionary = new Dictionary<string, ICloudEnvironmentSku>
            {
                ["test"] = skuMock.Object,
            };
            var skuCatalogMock = new Mock<ISkuCatalog>(MockBehavior.Strict);
            skuCatalogMock.Setup((sc) => sc.CloudEnvironmentSkus).Returns(skuDictionary);
            this.skuCatalog = skuCatalogMock.Object;
            this.environmentStateManager = new EnvironmentStateManager(billingEventManager, metricsLogger);
            this.environmentRepairWorkflows = new List<IEnvironmentRepairWorkflow>() { new ForceSuspendEnvironmentWorkflow(this.environmentStateManager, resourceBroker, environmentRepository) };
            this.resourceAllocationManager = new ResourceAllocationManager(this.resourceBroker);
            this.workspaceManager = new WorkspaceManager(this.workspaceRepository);
            this.subscriptionManager = new MockSubscriptionManager();
            this.resourceSelector = new ResourceSelectorFactory(this.skuCatalog, new Mock<ISystemConfiguration>().Object);

            this.environmentManager = new EnvironmentManager(
                this.environmentRepository,
                this.resourceBroker,
                this.tokenProvider,
                this.skuCatalog,
                this.environmentMonitor,
                new MockEnvironmentContinuation(),
                environmentSettings,
                planSettings,
                this.environmentStateManager,
                this.environmentRepairWorkflows,
                this.resourceAllocationManager,
                this.workspaceManager,
                this.subscriptionManager,
                this.resourceSelector);
        }

        public async Task<CloudEnvironmentServiceResult> CreateTestEnvironmentAsync(string name = "Test")
        {
            var serviceResult = await environmentManager.CreateAsync(
                new CloudEnvironment
                {
                    FriendlyName = name,
                    Location = AzureLocation.WestUs2,
                    ControlPlaneLocation = AzureLocation.CentralUs,
                    SkuName = "test",
                    PlanId = testPlan.ResourceId,
                    OwnerId = testUserId
                },
                new CloudEnvironmentOptions(),
                new StartCloudEnvironmentParameters
                {
                    UserProfile = MockProfile(),
                    FrontEndServiceUri = testServiceUri,
                    ConnectionServiceUri = testServiceUri,
                    CallbackUriFormat = testCallbackUriFormat,
                },
                testPlan,
                this.logger);

            return serviceResult;
        }

        public async Task MakeTestEnvironmentAvailableAsync(CloudEnvironment testEnvironment)
        {
            await this.environmentManager.UpdateCallbackAsync(
                testEnvironment,
                new EnvironmentRegistrationCallbackOptions
                {
                    Payload = new EnvironmentRegistrationCallbackPayloadOptions
                    {
                        SessionId = testEnvironment.Connection?.ConnectionSessionId,
                        SessionPath = "/",
                    },
                },
                this.logger);
        }


        public static ITokenProvider MockTokenProvider()
        {
            var moq = new Mock<ITokenProvider>();

            const string issuer = "test-issuer";
            const string audience = "test-audience";

            var key = new SymmetricSecurityKey(JwtTokenUtilities.GenerateKeyBytes(256));
            key.KeyId = Guid.NewGuid().ToString();
            var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwtWriter = new JwtWriter();
            jwtWriter.AddIssuer(issuer, signingCredentials);
            jwtWriter.AddAudience(audience);

            moq.Setup(obj => obj.IssueTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<IEnumerable<Claim>>(),
                It.IsAny<IDiagnosticsLogger>()))
                .Returns((
                    string issuer,
                    string audience,
                    DateTime expires,
                    IEnumerable<Claim> claims,
                    IDiagnosticsLogger logger)
                    => Task.FromResult(jwtWriter.WriteToken(
                        logger, issuer, audience, expires, claims.ToArray())));

            moq.Setup(obj => obj.Settings)
                .Returns(() => new AuthenticationSettings
                {
                    VsSaaSTokenSettings = new TokenSettings
                    {
                        Issuer = issuer,
                        Audience = audience,
                    },
                    ConnectionTokenSettings = new TokenSettings
                    {
                        Issuer = issuer,
                        Audience = audience,
                    },
                });

            return moq.Object;
        }

        public static Profile MockProfile(
            string provider = "mock-provider",
            string id = "mock-id",
            Dictionary<string, object> programs = null,
            string email = default)
        {
            return new Profile
            {
                Provider = provider,
                Id = id,
                Programs = programs,
                Email = email
            };
        }
    }
}
