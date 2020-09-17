using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.RepairWorkflows;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repository.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Test
{
    public class EnvironmentManagerSettingsUpdateTests
    {
        private static readonly IDiagnosticsLogger Logger = new DefaultLoggerFactory().New();

        [Fact]
        public async Task GetEnvironmentAvailableSettingsUpdates()
        {
            var sku1 = MockSku(skuName: "Sku1", skuTransitions: new[] { "Sku2" });
            var sku2 = MockSku(skuName: "Sku2");

            var skuCatalog = MockSkuCatalog(sku1, sku2);

            var environment = new CloudEnvironment()
            {
                Location = AzureLocation.WestUs2,
                ControlPlaneLocation = AzureLocation.CentralUs,
                SkuName = sku1.SkuName,
                Id = Guid.NewGuid().ToString(),
            };

            var expectedAutoShutdownOptions = new[] { 123, 456, };
            var manager = CreateManager(skuCatalog: skuCatalog, autoShutdownDelayOptions: expectedAutoShutdownOptions);

            var result = await manager.GetAvailableSettingsUpdatesAsync(environment, Logger);

            Assert.Equal(expectedAutoShutdownOptions, result.AllowedAutoShutdownDelayMinutes);
            Assert.Single(result.AllowedSkus);
            Assert.Equal(sku2, result.AllowedSkus[0]);
        }

        [Fact]
        public async Task UpdateEnvironmentSettings()
        {
            var targetSku = MockSku(skuName: "TargetSku");
            var activeSku = MockSku(skuName: "ActiveSku", skuTransitions: new[] { targetSku.SkuName });

            var skuCatalog = MockSkuCatalog(targetSku, activeSku);
            var environment = MockEnvironment(
                skuName: activeSku.SkuName,
                state: CloudEnvironmentState.Shutdown,
                autoShutdownDelayMinutes: 0);

            var regionalRepository = new MockRegionalCloudEnvironmentRepository(environment.Location);
            environment = await regionalRepository.CreateAsync(environment, Logger);

            var update = new CloudEnvironmentUpdate
            {
                AutoShutdownDelayMinutes = 30,
                SkuName = targetSku.SkuName,
            };

            var manager = CreateManager(regionalEnvironmentRepository: regionalRepository, skuCatalog: skuCatalog);
            var result = await manager.UpdateSettingsAsync(environment, update, Logger);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.CloudEnvironment);
            Assert.Equal(update.AutoShutdownDelayMinutes, result.CloudEnvironment.AutoShutdownDelayMinutes);
            Assert.Equal(update.SkuName, result.CloudEnvironment.SkuName);
        }

        [Fact]
        public async Task UpdateEnironmentSettings_NotFound()
        {
            var environmentRepository = new Mock<IRegionalCloudEnvironmentRepository>();
            environmentRepository
                .Setup(x => x.GetAsync(It.IsAny<DocumentDbKey>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((CloudEnvironment)null));

            var update = new CloudEnvironmentUpdate
            {
            };

            var manager = CreateManager(regionalEnvironmentRepository: environmentRepository.Object);

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await manager.UpdateSettingsAsync(null, update, Logger));
        }

        [Fact]
        public async Task UpdateEnironmentSettings_NotShutdown()
        {
            var sku = MockSku();
            var skuCatalog = MockSkuCatalog(sku);
            var environment = MockEnvironment(skuName: sku.SkuName, state: CloudEnvironmentState.Available);
            var environmentRepository = new MockRegionalCloudEnvironmentRepository(environment.Location);

            environment = await environmentRepository.CreateAsync(environment, Logger);

            var update = new CloudEnvironmentUpdate
            {
            };

            var manager = CreateManager(regionalEnvironmentRepository: environmentRepository, skuCatalog: skuCatalog);

            var result = await manager.UpdateSettingsAsync(environment, update, Logger);

            Assert.False(result.IsSuccess);
            Assert.Single(result.ValidationErrors);
            Assert.Equal(MessageCodes.EnvironmentNotShutdown, result.ValidationErrors.First());
        }

        [Fact]
        public async Task UpdateEnironmentSettings_InvalidAutoShutdown()
        {
            var sku = MockSku();
            var skuCatalog = MockSkuCatalog(sku);
            var environment = MockEnvironment(
                skuName: sku.SkuName,
                state: CloudEnvironmentState.Shutdown);
            var environmentRepository = new MockRegionalCloudEnvironmentRepository(environment.Location);

            environment = await environmentRepository.CreateAsync(environment, Logger);

            var update = new CloudEnvironmentUpdate
            {
                AutoShutdownDelayMinutes = -1,
            };

            var manager = CreateManager(regionalEnvironmentRepository: environmentRepository, skuCatalog: skuCatalog);

            var result = await manager.UpdateSettingsAsync(environment, update, Logger);

            Assert.False(result.IsSuccess);
            Assert.Single(result.ValidationErrors);
            Assert.Equal(MessageCodes.RequestedAutoShutdownDelayMinutesIsInvalid, result.ValidationErrors.First());
        }

        [Fact]
        public async Task UpdateEnironmentSettings_SkuUpdateNotAllowed()
        {
            var sku = MockSku();
            var skuCatalog = MockSkuCatalog(sku);
            var environment = MockEnvironment(
                skuName: sku.SkuName,
                state: CloudEnvironmentState.Shutdown);

            var environmentRepository = new MockRegionalCloudEnvironmentRepository(environment.Location);
            environment = await environmentRepository.CreateAsync(environment, Logger);

            var update = new CloudEnvironmentUpdate
            {
                SkuName = "some sku",
            };

            var manager = CreateManager(regionalEnvironmentRepository: environmentRepository, skuCatalog: skuCatalog);

            var result = await manager.UpdateSettingsAsync(environment, update, Logger);

            Assert.False(result.IsSuccess);
            Assert.Single(result.ValidationErrors);
            Assert.Equal(MessageCodes.UnableToUpdateSku, result.ValidationErrors.First());
        }

        [Fact]
        public async Task UpdateEnironmentSettings_InvalidSku()
        {
            var targetSku = MockSku(skuName: "TargetSku");
            var activeSku = MockSku(skuName: "ActiveSku", skuTransitions: new[] { targetSku.SkuName });
            var skuCatalog = MockSkuCatalog(targetSku, activeSku);
            var environment = MockEnvironment(
                skuName: activeSku.SkuName,
                state: CloudEnvironmentState.Shutdown);

            var environmentRepository = new MockRegionalCloudEnvironmentRepository(environment.Location);
            environment = await environmentRepository.CreateAsync(environment, Logger);

            var update = new CloudEnvironmentUpdate
            {
                SkuName = "bad sku name",
            };

            var manager = CreateManager(regionalEnvironmentRepository: environmentRepository, skuCatalog: skuCatalog);

            var result = await manager.UpdateSettingsAsync(environment, update, Logger);

            Assert.False(result.IsSuccess);
            Assert.Single(result.ValidationErrors);
            Assert.Equal(MessageCodes.RequestedSkuIsInvalid, result.ValidationErrors.First());
        }

        [Fact]
        public async Task RenameEnvironment()
        {
            var sku = MockSku();
            var skuCatalog = MockSkuCatalog(sku);
            var environment = MockEnvironment(skuName: sku.SkuName);
            var regionalRepository = new MockRegionalCloudEnvironmentRepository(environment.Location);

            environment = await regionalRepository.CreateAsync(environment, Logger);
            environment.State = CloudEnvironmentState.Shutdown;

            var update = new CloudEnvironmentUpdate
            {
                FriendlyName = "XYZ",
            };

            var manager = CreateManager(regionalEnvironmentRepository: regionalRepository, skuCatalog: skuCatalog);

            var result = await manager.UpdateSettingsAsync(environment, update, Logger);
            Assert.True(result.IsSuccess);
            Assert.Equal(update.FriendlyName, result.CloudEnvironment?.FriendlyName);
        }

        [Fact]
        public async Task RenameEnvironment_Conflict()
        {
            var planInfo = MockPlan();
            var sku = MockSku();
            var skuCatalog = MockSkuCatalog(sku);
            var environment1 = MockEnvironment(id: "env1", name: "name1", location: planInfo.Location, skuName: sku.SkuName, ownerId: "mock-profile-id", planId: planInfo.ResourceId);
            var environment2 = MockEnvironment(id: "env2", name: "name2", location: planInfo.Location, skuName: sku.SkuName, ownerId: "mock-profile-id", planId: planInfo.ResourceId);
            var environmentRepository = new MockRegionalCloudEnvironmentRepository(environment1.Location);
            var planRepository = new MockPlanRepository();
            var plan = new VsoPlan {
                Id = Guid.NewGuid().ToString(),
                Plan = planInfo,
            };

            await planRepository.CreateAsync(plan, Logger);

            environment1 = await environmentRepository.CreateAsync(environment1, Logger);
            environment2 = await environmentRepository.CreateAsync(environment2, Logger);
            environment1.State = CloudEnvironmentState.Shutdown;

            var update = new CloudEnvironmentUpdate
            {
                FriendlyName = environment2.FriendlyName,
            };

            var manager = CreateManager(regionalEnvironmentRepository: environmentRepository, skuCatalog: skuCatalog, planRepository: planRepository);

            var result = await manager.UpdateSettingsAsync(environment1, update, Logger);
            Assert.False(result.IsSuccess);
            Assert.Single(result.ValidationErrors);
            Assert.Equal(MessageCodes.EnvironmentNameAlreadyExists, result.ValidationErrors.Single());
        }

        [Fact]
        public async Task RenameEnvironment_Retry()
        {
            var sku = MockSku();
            var skuCatalog = MockSkuCatalog(sku);
            var environment = MockEnvironment(skuName: sku.SkuName);
            environment.State = CloudEnvironmentState.Shutdown;

            var regionalRepository = new Mock<IRegionalCloudEnvironmentRepository>();
            regionalRepository
                .SetupGet(x => x.ControlPlaneLocation)
                .Returns(environment.Location);

            regionalRepository
                .Setup(x => x.GetAsync(It.IsAny<DocumentDbKey>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(() => Task.FromResult(environment));

            int attempt = 0;
            regionalRepository
                .Setup(x => x.UpdateAsync(It.IsAny<CloudEnvironment>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns((CloudEnvironment cloudEnvironmentUpdate, IDiagnosticsLogger logger) =>
                {
                    if (++attempt < 2)
                    {
                        throw new Exception("Test retry exception.");
                    }

                    return Task.FromResult(cloudEnvironmentUpdate);
                });

            var update = new CloudEnvironmentUpdate
            {
                FriendlyName = "XYZ",
            };

            var manager = CreateManager(regionalEnvironmentRepository: regionalRepository.Object, skuCatalog: skuCatalog);

            var result = await manager.UpdateSettingsAsync(environment, update, Logger);
            Assert.True(result.IsSuccess);
            Assert.Equal(update.FriendlyName, result.CloudEnvironment?.FriendlyName);
        }

        [Fact]
        public async Task MoveEnvironment()
        {
            var sku = MockSku();
            var skuCatalog = MockSkuCatalog(sku);
            var billingEventRepository = new MockBillingEventRepository();

            var plan1 = MockPlan(name: "plan1");
            var plan2 = MockPlan(name: "plan2");

            var environment = MockEnvironment(skuName: sku.SkuName, planId: plan1.ResourceId);
            var globalRepository = new MockGlobalCloudEnvironmentRepository();
            var regionalRepository = new MockRegionalCloudEnvironmentRepository(environment.Location);
            var environmentOwnerId = environment.OwnerId;

            await globalRepository.CreateAsync(environment, Logger);
            environment = await regionalRepository.CreateAsync(environment, Logger);
            environment.State = CloudEnvironmentState.Shutdown;

            var update = new CloudEnvironmentUpdate
            {
                Plan = new VsoPlan { Plan = plan2, Tenant = Guid.Empty.ToString() },
                PlanAccessIdentity = new VsoClaimsIdentity(
                    authorizedPlan: plan2.ResourceId,
                    scopes: new[] { PlanAccessTokenScopes.WriteEnvironments },
                    authorizedEnvironments: null,
                    new ClaimsIdentity()),
            };

            var currentUserProvider = MockUtil.MockCurrentUserProvider(
                identity: new VsoClaimsIdentity(
                    authorizedPlan: plan1.ResourceId,
                    scopes: new[] { PlanAccessTokenScopes.WriteEnvironments },
                    authorizedEnvironments: null,
                    new ClaimsIdentity()));

            var manager = CreateManager(
                globalEnvironmentRepository: globalRepository,
                regionalEnvironmentRepository: regionalRepository,
                skuCatalog: skuCatalog,
                billingEventRepository: billingEventRepository,
                currentUserProvider: currentUserProvider);


            var result = await manager.UpdateSettingsAsync(environment, update, Logger);
            Assert.True(result.IsSuccess);
            Assert.Equal(update.Plan.Plan.ResourceId, result.CloudEnvironment?.PlanId);
            Assert.Equal(environmentOwnerId, environment.OwnerId);

            Assert.Collection(
                billingEventRepository.Values.OrderBy((e) => e.Time),
                (BillingEvent event0) =>
                {
                    Assert.Equal(BillingEventTypes.EnvironmentStateChange, event0.Type);
                    Assert.Equal(CloudEnvironmentState.Shutdown.ToString(),
                        ((BillingStateChange)event0.Args).OldValue);
                    Assert.Equal(CloudEnvironmentState.Moved.ToString(),
                        ((BillingStateChange)event0.Args).NewValue);
                },
                (BillingEvent event1) =>
                {
                    Assert.Equal(BillingEventTypes.EnvironmentStateChange, event1.Type);
                    Assert.Equal(CloudEnvironmentState.Moved.ToString(),
                        ((BillingStateChange)event1.Args).OldValue);
                    Assert.Equal(CloudEnvironmentState.Shutdown.ToString(),
                        ((BillingStateChange)event1.Args).NewValue);
                });
        }

        [Fact]
        public async Task MoveEnvironment_UpdateOwnerId()
        {
            var sku = MockSku();
            var skuCatalog = MockSkuCatalog(sku);
            var billingEventRepository = new MockBillingEventRepository();

            var planRepository = new MockPlanRepository();
            var plan1 = await planRepository.CreateAsync(new VsoPlan
            {
                Id = Guid.Empty.ToString(),
                Plan = MockPlan(name: "plan1"),
            }, Logger);
            var plan2 = await planRepository.CreateAsync(new VsoPlan
            {
                Id = Guid.Empty.ToString().Replace('0', '1'),
                Plan = MockPlan(name: "plan2"),
                Tenant = Guid.Empty.ToString().Replace('0', 'B'),
            }, Logger);

            var environment = MockEnvironment(skuName: sku.SkuName, planId: plan1.Plan.ResourceId);
            var regionalRepository = new MockRegionalCloudEnvironmentRepository(environment.Location);

            // Test with an owner ID that uses the plan ID as the tenant ID.
            environment.OwnerId = plan1.Id + "_" + Guid.Empty;

            environment = await regionalRepository.CreateAsync(environment, Logger);
            environment.State = CloudEnvironmentState.Shutdown;

            var update = new CloudEnvironmentUpdate
            {
                Plan = plan2,
            };

            var manager = CreateManager(
                regionalEnvironmentRepository: regionalRepository,
                skuCatalog: skuCatalog,
                planRepository: planRepository,
                billingEventRepository: billingEventRepository);

            var result = await manager.UpdateSettingsAsync(environment, update, Logger);
            Assert.True(result.IsSuccess);
            Assert.Equal(update.Plan.Plan.ResourceId, result.CloudEnvironment?.PlanId);
            Assert.StartsWith(plan2.Tenant, environment.OwnerId);
        }

        [Fact]
        public async Task MoveEnvironment_UpdateOwnerId2()
        {
            var sku = MockSku();
            var skuCatalog = MockSkuCatalog(sku);
            var billingEventRepository = new MockBillingEventRepository();

            var planRepository = new MockPlanRepository();
            var plan1 = await planRepository.CreateAsync(new VsoPlan
            {
                Id = Guid.Empty.ToString(),
                Plan = MockPlan(name: "plan1"),
                Tenant = Guid.Empty.ToString().Replace('0', 'A'),
            }, Logger);
            var plan2 = await planRepository.CreateAsync(new VsoPlan
            {
                Id = Guid.Empty.ToString().Replace('0', '1'),
                Plan = MockPlan(name: "plan2"),
                Tenant = Guid.Empty.ToString().Replace('0', 'B'),
            }, Logger);

            var environment = MockEnvironment(skuName: sku.SkuName, planId: plan1.Plan.ResourceId);
            var regionalRepository = new MockRegionalCloudEnvironmentRepository(environment.Location);

            // Test with an owner ID that uses the plan's Tenant property as the tenant ID.
            environment.OwnerId = plan1.Tenant + "_" + Guid.Empty;

            environment = await regionalRepository.CreateAsync(environment, Logger);
            environment.State = CloudEnvironmentState.Shutdown;

            var update = new CloudEnvironmentUpdate
            {
                Plan = plan2,
            };

            var manager = CreateManager(
                regionalEnvironmentRepository: regionalRepository,
                skuCatalog: skuCatalog,
                planRepository: planRepository,
                billingEventRepository: billingEventRepository);

            var result = await manager.UpdateSettingsAsync(environment, update, Logger);
            Assert.True(result.IsSuccess);
            Assert.Equal(update.Plan.Plan.ResourceId, result.CloudEnvironment?.PlanId);
            Assert.StartsWith(plan2.Tenant, environment.OwnerId);
        }


        [Fact]
        public async Task MoveEnvironment_Conflict()
        {
            var sku = MockSku();
            var skuCatalog = MockSkuCatalog(sku);
            var billingEventRepository = new MockBillingEventRepository();

            var plan1Info = MockPlan(name: "plan1");
            var plan1 = new VsoPlan {
                Id = Guid.NewGuid().ToString(),
                Plan = plan1Info,
            };
            var plan2Info = MockPlan(name: "plan2");
            var plan2 = new VsoPlan {
                Id = Guid.NewGuid().ToString(),
                Plan = plan2Info,
            };

            var environment1 = MockEnvironment(id: "env1", name: "name1", planId: plan1Info.ResourceId, location: plan1Info.Location, skuName: sku.SkuName, ownerId: "mock-profile-id");
            var environment2 = MockEnvironment(id: "env2", name: "name2", planId: plan2Info.ResourceId, location: plan2Info.Location, skuName: sku.SkuName, ownerId: "mock-profile-id");
            var environmentRepository = new MockRegionalCloudEnvironmentRepository(environment1.Location);
            var planRepository = new MockPlanRepository();

            await planRepository.CreateAsync(plan1, Logger);
            await planRepository.CreateAsync(plan2, Logger);

            environment1 = await environmentRepository.CreateAsync(environment1, Logger);
            environment2 = await environmentRepository.CreateAsync(environment2, Logger);
            environment1.State = CloudEnvironmentState.Shutdown;

            var update = new CloudEnvironmentUpdate
            {
                FriendlyName = environment2.FriendlyName,
                Plan = new VsoPlan { Plan = plan2Info, Tenant = Guid.Empty.ToString() },
            };

            var manager = CreateManager(
                regionalEnvironmentRepository: environmentRepository,
                skuCatalog: skuCatalog,
                planRepository: planRepository,
                billingEventRepository: billingEventRepository);

            var result = await manager.UpdateSettingsAsync(environment1, update, Logger);
            Assert.False(result.IsSuccess);
            Assert.Single(result.ValidationErrors);
            Assert.Equal(MessageCodes.EnvironmentNameAlreadyExists, result.ValidationErrors.Single());
        }

        [Fact]
        public async Task MoveEnvironment_ExceededQuota()
        {
            var sku = MockSku();
            var skuCatalog = MockSkuCatalog(sku);
            var billingEventRepository = new MockBillingEventRepository();

            var plan1 = MockPlan(name: "plan1");
            var plan2 = MockPlan(name: "plan2");

            var environment1 = MockEnvironment(id: "env1", name: "name1", planId: plan1.ResourceId, skuName: sku.SkuName);
            var environment2 = MockEnvironment(id: "env2", name: "name2", planId: plan2.ResourceId, skuName: sku.SkuName);
            var environmentRepository = new MockRegionalCloudEnvironmentRepository(environment1.Location);

            environment1 = await environmentRepository.CreateAsync(environment1, Logger);
            environment2 = await environmentRepository.CreateAsync(environment2, Logger);
            environment1.State = CloudEnvironmentState.Shutdown;

            var update = new CloudEnvironmentUpdate
            {
                Plan = new VsoPlan { Plan = plan2, Tenant = Guid.Empty.ToString() },
            };

            // Configure max 1 env per plan so quota will be exceeded when moving.
            var environmentSettings = new EnvironmentManagerSettings()
            {
                DefaultMaxEnvironmentsPerPlan = 1,
            };

            var manager = CreateManager(
                regionalEnvironmentRepository: environmentRepository,
                skuCatalog: skuCatalog,
                billingEventRepository: billingEventRepository,
                environmentSettings: environmentSettings);

            var result = await manager.UpdateSettingsAsync(environment1, update, Logger);
            Assert.False(result.IsSuccess);
            Assert.Equal(MessageCodes.ExceededQuota, result.ValidationErrors.Single());
        }

        private EnvironmentManager CreateManager(
            IGlobalCloudEnvironmentRepository globalEnvironmentRepository = null,
            IRegionalCloudEnvironmentRepository regionalEnvironmentRepository = null,
            ISkuCatalog skuCatalog = null,
            IPlanRepository planRepository = null,
            IBillingEventRepository billingEventRepository = null,
            ICurrentUserProvider currentUserProvider = null,
            EnvironmentManagerSettings environmentSettings = null,
            int[] autoShutdownDelayOptions = null)
        {
            var defaultCount = 20;
            var defaultAutoShutdownOptions = new[] { 0, 5, 30, 120 };
            var planSettings = new PlanManagerSettings()
            {
                DefaultMaxPlansPerSubscription = defaultCount,
                DefaultAutoSuspendDelayMinutesOptions = autoShutdownDelayOptions ?? defaultAutoShutdownOptions,
            };
            environmentSettings ??= new EnvironmentManagerSettings()
            {
                DefaultMaxEnvironmentsPerPlan = defaultCount,
            };

            var mockSystemConfiguration = new Mock<ISystemConfiguration>();
            mockSystemConfiguration
                .Setup(x => x.GetValueAsync<int>(It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>(), defaultCount))
                .Returns(Task.FromResult(defaultCount));

            planSettings.Init(mockSystemConfiguration.Object);
            environmentSettings.Init(mockSystemConfiguration.Object);

            planRepository ??= new MockPlanRepository();
            globalEnvironmentRepository = globalEnvironmentRepository ?? new MockGlobalCloudEnvironmentRepository();
            regionalEnvironmentRepository = regionalEnvironmentRepository ?? new MockRegionalCloudEnvironmentRepository();
            var repoFactory = new Mock<IRegionalCloudEnvironmentRepositoryFactory>();
            var environmentRepository = new CloudEnvironmentRepository(planRepository, repoFactory.Object, globalEnvironmentRepository, regionalEnvironmentRepository, null);
            currentUserProvider ??= MockUtil.MockCurrentUserProvider();
            billingEventRepository ??= new MockBillingEventRepository();
            skuCatalog ??= MockSkuCatalog();

            var billingEventManager = new BillingEventManager(billingEventRepository, new MockBillingOverrideRepository());
            var workspaceRepository = new MockClientWorkspaceRepository();
            var tokenProvider = MockUtil.MockTokenProvider();
            var resourceBroker = new MockResourceBrokerClient();
            var environmentMonitor = new MockEnvironmentMonitor();
            var metricsLogger = new MockEnvironmentMetricsLogger();
            var environmentContinuation = new Mock<IEnvironmentContinuationOperations>().Object;
            var workspaceManager = new WorkspaceManager(workspaceRepository);
            var currentLocationProvider = new Mock<ICurrentLocationProvider>().Object;
            var planManager = new PlanManager(planRepository, planSettings, skuCatalog, currentLocationProvider);
            var environmentStateChangeManager = new Mock<IEnvironmentStateChangeManager>().Object;
            var environmentStateManager = new EnvironmentStateManager(workspaceManager, environmentRepository, billingEventManager, environmentStateChangeManager, metricsLogger);
            var serviceProvider = new Mock<IServiceProvider>();
            var environmentRepairWorkflows = new List<IEnvironmentRepairWorkflow>() { new ForceSuspendEnvironmentWorkflow(environmentStateManager, resourceBroker, environmentRepository, serviceProvider.Object) };
            var resourceAllocationManager = new ResourceAllocationManager(resourceBroker);
            var subscriptionManager = new MockSubscriptionManager();
            var systemConfiguration = new Mock<ISystemConfiguration>().Object;
            var environmentContinuationOperations = new Mock<IEnvironmentContinuationOperations>(MockBehavior.Loose);
            var resourceStartManager = new Mock<IResourceStartManager>().Object;

            var environmentGetAction = new Mock<IEnvironmentGetAction>().Object;
            var environmentUpdateStatusAction = new Mock<IEnvironmentUpdateStatusAction>().Object;
            var environmentCreateAction = new Mock<IEnvironmentCreateAction>().Object;
            var environmentDeleteRestoreAction = new Mock<IEnvironmentDeleteRestoreAction>().Object;
            var environmentListAction = new EnvironmentListAction(
                environmentRepository,
                MockUtil.MockCurrentLocationProvider(),
                currentUserProvider,
                MockUtil.MockControlPlaneInfo(),
                environmentSettings);
            var environmentResumeAction = new Mock<IEnvironmentResumeAction>().Object;
            var environmentIntializeResumeAction = new Mock<IEnvironmentIntializeResumeAction>().Object;
            var environmentIntializeExportAction = new Mock<IEnvironmentIntializeExportAction>().Object;
            var environmentExportAction = new Mock<IEnvironmentExportAction>().Object;
            var environmentFinalizeResumeAction = new Mock<IEnvironmentFinalizeResumeAction>().Object;
            var environmentFinalizeExportAction = new Mock<IEnvironmentFinalizeExportAction>().Object;
            var environmentDeleteAction = new Mock<IEnvironmentHardDeleteAction>().Object;
            var environmentSuspendAction = new Mock<IEnvironmentSuspendAction>().Object;
            var environmentForceSuspendAction = new Mock<IEnvironmentForceSuspendAction>().Object;
            var environmentSoftDeleteAction = new Mock<IEnvironmentSoftDeleteAction>().Object;

            var resourceSelector = new ResourceSelectorFactory(skuCatalog, systemConfiguration);

            return new EnvironmentManager(
                environmentRepository,
                resourceBroker,
                skuCatalog,
                environmentContinuationOperations.Object,
                environmentSettings,
                planManager,
                planSettings,
                environmentStateManager,
                resourceStartManager,
                environmentGetAction,
                environmentListAction,
                environmentUpdateStatusAction,
                environmentCreateAction,
                environmentDeleteRestoreAction,
                environmentIntializeResumeAction,
                environmentIntializeExportAction,
                environmentFinalizeResumeAction,
                environmentFinalizeExportAction,
                environmentSuspendAction,
                environmentForceSuspendAction,
                environmentDeleteAction,
                environmentSoftDeleteAction);
        }

        private static ICloudEnvironmentSku MockSku(
            string skuName = "MockSkuName",
            string displayName = "MockSkuDisplayName",
            ComputeOS computeOs = ComputeOS.Linux,
            decimal storageUnits = 2,
            decimal computeUnits = 125,
            IEnumerable<string> skuTransitions = null)
        {
            skuTransitions = skuTransitions ?? new List<string>();

            var currentImageInfoProvider = new Mock<ICurrentImageInfoProvider>();
            currentImageInfoProvider
                .Setup(x => x.GetImageNameAsync(It.IsAny<ImageFamilyType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns((ImageFamilyType familyType, string family, string defaultName, IDiagnosticsLogger logger) => Task.FromResult(defaultName));
            currentImageInfoProvider
                .Setup(x => x.GetImageVersionAsync(It.IsAny<ImageFamilyType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns((ImageFamilyType familyType, string family, string defaultVersion, IDiagnosticsLogger logger) => Task.FromResult(defaultVersion));

            return new CloudEnvironmentSku(
                skuName,
                SkuTier.Standard,
                displayName,
                true,
                new[] { AzureLocation.WestUs2 },
                "computeSkuFamily",
                "computeSkuName",
                "computeSkuSize",
                4,
                computeOs,
                new BuildArtifactImageFamily(
                    ImageFamilyType.VmAgent,
                    "agentImageFamily",
                    "agentImageName",
                    "computeImageVersion",
                    currentImageInfoProvider.Object),
                new VmImageFamily(
                    MockControlPlaneStampInfo(),
                    "vmImageFamilyName",
                    ImageKind.Canonical,
                    "vmImageName",
                    "vmImageVersion",
                    "vsVersion",
                    "vmImageSubscriptionId",
                    currentImageInfoProvider.Object),
                    "storageSkuName",
                new BuildArtifactImageFamily(
                    ImageFamilyType.Storage,
                    "storageImageFamily",
                    "storageImageName",
                    null,
                    currentImageInfoProvider.Object),
                64,
                storageUnits,
                computeUnits,
                5,
                5,
                new ReadOnlyCollection<string>(skuTransitions.ToList()),
                new ReadOnlyCollection<string>(skuTransitions.ToList()),
                1);
        }

        private static ISkuCatalog MockSkuCatalog(params ICloudEnvironmentSku[] skus)
        {
            var skuDict = new ReadOnlyDictionary<string, ICloudEnvironmentSku>(skus.ToDictionary((s) => s.SkuName));

            var moq = new Mock<ISkuCatalog>();
            moq
                .Setup(obj => obj.CloudEnvironmentSkus)
                .Returns(skuDict);

            return moq.Object;
        }

        private static IControlPlaneStampInfo MockControlPlaneStampInfo()
        {
            return new Mock<IControlPlaneStampInfo>().Object;
        }

        public CloudEnvironment MockEnvironment(
            string name = "env-name",
            string id = "env-id",
            AzureLocation location = AzureLocation.WestUs2,
            string planId = null,
            string ownerId = "owner-id",
            string skuName = "sku-name",
            CloudEnvironmentState state = CloudEnvironmentState.Available,
            int autoShutdownDelayMinutes = 0)
        {
            return new CloudEnvironment
            {
                Id = id,
                FriendlyName = name,
                Location = location,
                ControlPlaneLocation = location,
                SkuName = skuName,
                PlanId = planId ?? MockPlan().ResourceId,
                OwnerId = ownerId,
                State = state,
                AutoShutdownDelayMinutes = autoShutdownDelayMinutes,
                IsMigrated = true
            };
        }

        public VsoPlanInfo MockPlan(
            string subscription = "00000000-0000-0000-0000-000000000000",
            string resourceGroup = "test-resourcegroup",
            string name = "test-plan",
            AzureLocation location = AzureLocation.WestUs2)
        {
            return new VsoPlanInfo
            {
                Subscription = subscription,
                ResourceGroup = resourceGroup,
                Name = name,
                Location = location,
            };
        }
    }
}
