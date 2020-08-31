using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.RepairWorkflows;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repository.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveshareAuthentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using Moq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Test
{
    public class EnvironmentManagerTestsBase
    {
        public readonly MockGlobalCloudEnvironmentRepository globalEnvironmentRepository;
        public readonly MockRegionalCloudEnvironmentRepository regionalEnvironmentRepository;
        public readonly CloudEnvironmentRepository environmentRepository;
        public readonly MockPlanRepository planRepository;
        public readonly MockBillingEventRepository billingEventRepository;
        public readonly MockClientWorkspaceRepository workspaceRepository;
        public readonly IBillingEventManager billingEventManager;
        public readonly MockClientAuthRepository authRepository;
        public readonly IResourceBrokerResourcesExtendedHttpContract resourceBroker;
        public readonly ITokenProvider tokenProvider;
        public readonly EnvironmentManager environmentManager;
        private readonly IResourceStartManager resourceStartManager;
        private readonly IEnvironmentUpdateStatusAction environmentUpdateStatusAction;
        public readonly IDiagnosticsLoggerFactory loggerFactory;
        public readonly IDiagnosticsLogger logger;
        public readonly ISkuCatalog skuCatalog;
        private readonly List<IEnvironmentRepairWorkflow> environmentRepairWorkflows;
        private readonly IResourceAllocationManager resourceAllocationManager;
        private readonly IWorkspaceManager workspaceManager;
        public readonly IEnvironmentMonitor environmentMonitor;
        public readonly IEnvironmentStateManager environmentStateManager;
        public readonly ICurrentLocationProvider currentLocationProvider;
        public readonly Mock<IEnvironmentContinuationOperations> environmentContinuationOperations = new Mock<IEnvironmentContinuationOperations>(MockBehavior.Loose);
        private IEnvironmentAccessManager environmentAccessManager;
        private IEnvironmentSubscriptionManager environmentSubscriptionManager;
        private IMapper mapper;
        private IEnvironmentArchivalTimeCalculator environmentArchivalTimeCalculator;
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

        public Subscription Subscription = new Subscription
        {
            Id = testPlan.Subscription,
            QuotaId = "testQuotaValue",
            CurrentMaximumQuota = new Dictionary<string, int>
            {
                {"standardDSv3Family", 10 },
                {"standardFSv2Family", 10 },
                {"computeSkuFamily", 10 },
            }
        };

        public EnvironmentManagerTestsBase()
        {
            loggerFactory = new DefaultLoggerFactory();
            logger = loggerFactory.New();

            var defaultCount = 20;
            var softDeleteFeatureFlagKey = "featureflag:environment-soft-delete-enabled";
            var planSettings = new PlanManagerSettings()
            {
                DefaultMaxPlansPerSubscription = defaultCount,
                DefaultAutoSuspendDelayMinutesOptions = new int[] { 0, 5, 30, 120 },
            };
            var environmentSettings = new EnvironmentManagerSettings()
            {
                DefaultMaxEnvironmentsPerPlan = defaultCount,
                DefaultComputeCheckEnabled = false,
                DefaultEnvironmentSoftDeleteEnabled = true,
            };

            var mockSystemConfiguration = new Mock<ISystemConfiguration>();
            mockSystemConfiguration
                .Setup(x => x.GetValueAsync<int>(It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>(), defaultCount))
                .Returns(Task.FromResult(defaultCount));

            mockSystemConfiguration
                .Setup(x => x.GetValueAsync(softDeleteFeatureFlagKey, It.IsAny<IDiagnosticsLogger>(), It.IsAny<bool>()))
                .Returns(Task.FromResult(true));

            planSettings.Init(mockSystemConfiguration.Object);
            environmentSettings.Init(mockSystemConfiguration.Object);

            this.globalEnvironmentRepository = new MockGlobalCloudEnvironmentRepository();
            this.regionalEnvironmentRepository = new MockRegionalCloudEnvironmentRepository();
            this.environmentRepository = new CloudEnvironmentRepository(this.globalEnvironmentRepository, this.regionalEnvironmentRepository);
            this.planRepository = new MockPlanRepository();
            this.billingEventRepository = new MockBillingEventRepository();
            this.billingEventManager = new BillingEventManager(this.billingEventRepository,
                                                                new MockBillingOverrideRepository());
            this.workspaceRepository = new MockClientWorkspaceRepository();
            this.tokenProvider = MockUtil.MockTokenProvider();
            this.resourceBroker = new MockResourceBrokerClient();
            this.environmentMonitor = new MockEnvironmentMonitor();
            var metricsLogger = new MockEnvironmentMetricsLogger();

            var skuMock = new Mock<ICloudEnvironmentSku>(MockBehavior.Strict);
            var skuWindowsMock = new Mock<ICloudEnvironmentSku>(MockBehavior.Strict);
            skuMock.Setup((s) => s.ComputeOS).Returns(ComputeOS.Linux);
            skuMock.Setup(s => s.ComputeSkuCores).Returns(defaultCount);
            skuMock.Setup(s => s.ComputeSkuFamily).Returns("standardDSv3Family");
            skuWindowsMock.Setup((s) => s.ComputeOS).Returns(ComputeOS.Windows);
            skuWindowsMock.Setup(s => s.ComputeSkuCores).Returns(1);
            skuWindowsMock.Setup(s => s.ComputeSkuFamily).Returns("standardFSv2Family");
            var skuDictionary = new Dictionary<string, ICloudEnvironmentSku>
            {
                ["test"] = skuMock.Object,
                ["windows"] = skuWindowsMock.Object,
            };
            var skuCatalogMock = new Mock<ISkuCatalog>(MockBehavior.Strict);
            skuCatalogMock.Setup((sc) => sc.CloudEnvironmentSkus).Returns(skuDictionary);
            this.skuCatalog = skuCatalogMock.Object;
            this.workspaceManager = new WorkspaceManager(this.workspaceRepository);
            this.currentLocationProvider = new Mock<ICurrentLocationProvider>().Object;
            var planManager = new PlanManager(this.planRepository, planSettings, this.skuCatalog, currentLocationProvider);
            var environmentStateChangeManager = new Mock<IEnvironmentStateChangeManager>().Object;

            this.environmentStateManager = new EnvironmentStateManager(workspaceManager, environmentRepository, billingEventManager, environmentStateChangeManager, metricsLogger);

            var serviceProvider = new Mock<IServiceProvider>().Object;
            this.environmentRepairWorkflows = new List<IEnvironmentRepairWorkflow>() { new ForceSuspendEnvironmentWorkflow(this.environmentStateManager, resourceBroker, this.environmentRepository, serviceProvider) };
            this.resourceAllocationManager = new ResourceAllocationManager(this.resourceBroker);
            this.resourceStartManager = new Mock<IResourceStartManager>().Object;
            this.environmentAccessManager = new Mock<IEnvironmentAccessManager>().Object;
            this.environmentSubscriptionManager = new EnvironmentSubscriptionManager(this.environmentRepository, skuCatalog);

            this.environmentUpdateStatusAction = new Mock<IEnvironmentUpdateStatusAction>().Object;
            this.environmentArchivalTimeCalculator = new Mock<IEnvironmentArchivalTimeCalculator>().Object;

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<EnvironmentCreateDetails, CloudEnvironment>();
            });
            this.mapper = config.CreateMapper();

            var taskHelper = new Mock<ITaskHelper>().Object;
            var environmentActionValidator = new EnvironmentActionValidator(
                MockUtil.MockSubscriptionManager(),
                MockUtil.MockSkuCatalog(),
                environmentSubscriptionManager,
                environmentSettings
                );
            var environmentListAction = new EnvironmentListAction(
               this.environmentRepository,
               MockUtil.MockCurrentLocationProvider(),
               MockUtil.MockCurrentUserProvider(),
               MockUtil.MockControlPlaneInfo(),
               environmentSettings);

            var environmentDeleteAction = new EnvironmentHardDeleteAction(
                environmentStateManager,
                environmentRepository,
                MockUtil.MockCurrentLocationProvider(),
                MockUtil.MockCurrentUserProvider(),
                MockUtil.MockControlPlaneInfo(),
                environmentAccessManager,
                resourceBroker,
                workspaceManager,
                environmentListAction
                );

            var environmentCreateAction = new EnvironmentCreateAction(
                MockUtil.MockPlanManager(() => MockUtil.GeneratePlan()),
                MockUtil.MockSkuCatalog(),
                MockUtil.MockSkuUtils(true),
                environmentListAction,
                environmentSettings,
                new PlanManagerSettings { DefaultAutoSuspendDelayMinutesOptions = new int[] { 5, 30, 60, 120 } },
                workspaceManager,
                environmentMonitor,
                environmentContinuationOperations.Object,
                resourceAllocationManager,
                resourceStartManager,
                MockUtil.MockResourceSelectorFactory(),
                environmentStateManager,
                environmentRepository,
                MockUtil.MockCurrentLocationProvider(),
                MockUtil.MockCurrentUserProvider(),
                MockUtil.MockControlPlaneInfo(),
                environmentAccessManager,
                environmentDeleteAction,
                mapper,
                environmentActionValidator,
                mockSystemConfiguration.Object);

            var environmentGetAction = new EnvironmentGetAction(
                environmentStateManager,
                environmentRepository,
                MockUtil.MockCurrentLocationProvider(),
                MockUtil.MockCurrentUserProvider(),
                MockUtil.MockControlPlaneInfo(),
                environmentAccessManager,
                MockUtil.MockSkuCatalog(),
                MockUtil.MockSkuUtils(true));

            var environmentDeleteRestoreAction = new EnvironmentDeleteRestoreAction(
                environmentStateManager,
                environmentRepository,
                MockUtil.MockCurrentLocationProvider(),
                MockUtil.MockCurrentUserProvider(),
                MockUtil.MockControlPlaneInfo(),
                environmentAccessManager,
                environmentListAction,
                MockUtil.MockPlanManager(() => MockUtil.GeneratePlan()),
                environmentActionValidator,
                environmentSettings);

            var environmentForceSuspendAction = new EnvironmentForceSuspendAction(
              environmentStateManager,
              environmentRepository,
              MockUtil.MockCurrentLocationProvider(),
              MockUtil.MockCurrentUserProvider(),
              MockUtil.MockControlPlaneInfo(),
              environmentAccessManager,
              MockUtil.MockSkuCatalog(),
              MockUtil.MockSkuUtils(true),
              environmentContinuationOperations.Object,
              resourceBroker
              );

            var environmentSuspendAction = new EnvironmentSuspendAction(
                environmentStateManager,
                environmentRepository,
                MockUtil.MockCurrentLocationProvider(),
                MockUtil.MockCurrentUserProvider(),
                MockUtil.MockControlPlaneInfo(),
                environmentAccessManager,
                MockUtil.MockSkuCatalog(),
                MockUtil.MockSkuUtils(true),
                resourceBroker,
                environmentMonitor,
                environmentForceSuspendAction,
                environmentSettings,
                environmentArchivalTimeCalculator
                );

            var environmentResumeAction = new EnvironmentResumeAction(
                environmentStateManager,
                environmentRepository,
                MockUtil.MockCurrentLocationProvider(),
                MockUtil.MockCurrentUserProvider(),
                MockUtil.MockControlPlaneInfo(),
                environmentAccessManager,
                MockUtil.MockSkuCatalog(),
                MockUtil.MockSkuUtils(true),
                MockUtil.MockPlanManager(() => MockUtil.GeneratePlan()),
                workspaceManager,
                environmentMonitor,
                environmentContinuationOperations.Object,
                resourceAllocationManager,
                resourceStartManager,
                environmentSuspendAction,
                resourceBroker,
                taskHelper
                );

            var environmentExportAction = new EnvironmentExportAction(
                environmentStateManager,
                environmentRepository,
                MockUtil.MockCurrentLocationProvider(),
                MockUtil.MockCurrentUserProvider(),
                MockUtil.MockControlPlaneInfo(),
                environmentAccessManager,
                MockUtil.MockSkuCatalog(),
                MockUtil.MockSkuUtils(true),
                MockUtil.MockPlanManager(() => MockUtil.GeneratePlan()),
                workspaceManager,
                environmentMonitor,
                environmentContinuationOperations.Object,
                resourceAllocationManager,
                resourceStartManager,
                environmentSuspendAction,
                resourceBroker,
                taskHelper
                );

            var environmentIntializeResumeAction = new EnvironmentIntializeResumeAction(
                environmentStateManager,
                environmentRepository,
                MockUtil.MockCurrentLocationProvider(),
                MockUtil.MockCurrentUserProvider(),
                MockUtil.MockControlPlaneInfo(),
                environmentAccessManager,
                environmentResumeAction,
                MockUtil.MockSkuCatalog(),
                MockUtil.MockSkuUtils(true),
                MockUtil.MockPlanManager(() => MockUtil.GeneratePlan()),
                MockUtil.MockSubscriptionManager(),
                environmentSubscriptionManager,
                environmentSettings
                );

            var environmentIntializeExportAction = new EnvironmentIntializeExportAction(
                environmentStateManager,
                environmentRepository,
                MockUtil.MockCurrentLocationProvider(),
                MockUtil.MockCurrentUserProvider(),
                MockUtil.MockControlPlaneInfo(),
                environmentAccessManager,
                environmentExportAction,
                MockUtil.MockSkuCatalog(),
                MockUtil.MockSkuUtils(true),
                MockUtil.MockPlanManager(() => MockUtil.GeneratePlan()),
                MockUtil.MockSubscriptionManager(),
                environmentSubscriptionManager,
                environmentSettings);

            var environmentFinalizeResumeAction = new EnvironmentFinalizeResumeAction(
                environmentStateManager,
                environmentRepository,
                MockUtil.MockCurrentLocationProvider(),
                MockUtil.MockCurrentUserProvider(),
                MockUtil.MockControlPlaneInfo(),
                environmentAccessManager,
                MockUtil.MockSkuCatalog(),
                MockUtil.MockSkuUtils(true),
                resourceBroker
                );

            var environmentSoftDeleteAction = new EnvironmentSoftDeleteAction(
                environmentStateManager,
                environmentRepository,
                MockUtil.MockCurrentLocationProvider(),
                MockUtil.MockCurrentUserProvider(),
                MockUtil.MockControlPlaneInfo(),
                environmentAccessManager,
                environmentSuspendAction);

            var environmentFinalizeExportAction = new EnvironmentFinalizeExportAction(
                environmentStateManager,
                environmentRepository,
                MockUtil.MockCurrentLocationProvider(),
                MockUtil.MockCurrentUserProvider(),
                MockUtil.MockControlPlaneInfo(),
                environmentAccessManager,
                MockUtil.MockSkuCatalog(),
                MockUtil.MockSkuUtils(true),
                resourceBroker
                );

            this.environmentManager = new EnvironmentManager(
                this.environmentRepository,
                this.resourceBroker,
                MockUtil.MockSkuCatalog(),
                environmentContinuationOperations.Object,
                environmentSettings,
                planManager,
                planSettings,
                this.environmentStateManager,
                this.resourceStartManager,
                environmentGetAction,
                environmentListAction,
                this.environmentUpdateStatusAction,
                environmentCreateAction,
                environmentDeleteRestoreAction,
                environmentIntializeResumeAction,
                environmentIntializeExportAction,
                environmentFinalizeResumeAction,
                environmentFinalizeExportAction,
                environmentSuspendAction,
                environmentForceSuspendAction,
                environmentDeleteAction,
                environmentSoftDeleteAction
                );
        }

        public async Task<CloudEnvironment> CreateTestEnvironmentAsync(string name = "Test", string skuName = "testSkuName")
        {

            var cloudEnvironment = await environmentManager.CreateAsync(
                new EnvironmentCreateDetails
                {
                    FriendlyName = name,
                    SkuName = skuName,
                    PlanId = testPlan.ResourceId,
                    AutoShutdownDelayMinutes = 30,
                },
                new StartCloudEnvironmentParameters
                {
                    UserProfile = MockUtil.MockProfile(),
                    FrontEndServiceUri = testServiceUri,
                    ConnectionServiceUri = testServiceUri,
                    CallbackUriFormat = testCallbackUriFormat,
                },
                new MetricsInfo(),
                this.logger);

            return cloudEnvironment;
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
    }
}
