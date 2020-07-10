using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyModel;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.RepairWorkflows;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repository.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;
using Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions.Mocks;
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
        public async Task UpdateEnironmentSettings()
        {
            var targetSku = MockSku(skuName: "TargetSku");
            var activeSku = MockSku(skuName: "ActiveSku", skuTransitions: new[] { targetSku.SkuName });

            var skuCatalog = MockSkuCatalog(targetSku, activeSku);

            var environmentRepository = new MockCloudEnvironmentRepository();

            var environment = MockEnvironment(
                skuName: activeSku.SkuName,
                state: CloudEnvironmentState.Shutdown,
                autoShutdownDelayMinutes: 0);

            environment = await environmentRepository.CreateAsync(environment, Logger);

            var update = new CloudEnvironmentUpdate
            {
                AutoShutdownDelayMinutes = 30,
                SkuName = targetSku.SkuName,
            };

            var manager = CreateManager(environmentRepository: environmentRepository, skuCatalog: skuCatalog);

            var result = await manager.UpdateSettingsAsync(environment, update, null, Logger);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.CloudEnvironment);
            Assert.Equal(update.AutoShutdownDelayMinutes, result.CloudEnvironment.AutoShutdownDelayMinutes);
            Assert.Equal(update.SkuName, result.CloudEnvironment.SkuName);
        }

        [Fact]
        public async Task UpdateEnironmentSettings_NotFound()
        {
            var environmentRepository = new Mock<ICloudEnvironmentRepository>();
            environmentRepository
                .Setup(x => x.GetAsync(It.IsAny<DocumentDbKey>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((CloudEnvironment)null));

            var update = new CloudEnvironmentUpdate
            {
            };

            var manager = CreateManager(environmentRepository: environmentRepository.Object);

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await manager.UpdateSettingsAsync(null, update, null, Logger));
        }

        [Fact]
        public async Task UpdateEnironmentSettings_NotShutdown()
        {
            var sku = MockSku();
            var skuCatalog = MockSkuCatalog(sku);

            var environmentRepository = new MockCloudEnvironmentRepository();

            var environment = MockEnvironment(skuName: sku.SkuName, state: CloudEnvironmentState.Available);
            environment = await environmentRepository.CreateAsync(environment, Logger);

            var update = new CloudEnvironmentUpdate
            {
            };

            var manager = CreateManager(environmentRepository: environmentRepository, skuCatalog: skuCatalog);

            var result = await manager.UpdateSettingsAsync(environment, update, null, Logger);

            Assert.False(result.IsSuccess);
            Assert.Single(result.ValidationErrors);
            Assert.Equal(MessageCodes.EnvironmentNotShutdown, result.ValidationErrors.First());
        }

        [Fact]
        public async Task UpdateEnironmentSettings_InvalidAutoShutdown()
        {
            var sku = MockSku();

            var skuCatalog = MockSkuCatalog(sku);

            var environmentRepository = new MockCloudEnvironmentRepository();

            var environment = MockEnvironment(
                skuName: sku.SkuName,
                state: CloudEnvironmentState.Shutdown);

            environment = await environmentRepository.CreateAsync(environment, Logger);

            var update = new CloudEnvironmentUpdate
            {
                AutoShutdownDelayMinutes = -1,
            };

            var manager = CreateManager(environmentRepository: environmentRepository, skuCatalog: skuCatalog);

            var result = await manager.UpdateSettingsAsync(environment, update, null, Logger);

            Assert.False(result.IsSuccess);
            Assert.Single(result.ValidationErrors);
            Assert.Equal(MessageCodes.RequestedAutoShutdownDelayMinutesIsInvalid, result.ValidationErrors.First());
        }

        [Fact]
        public async Task UpdateEnironmentSettings_SkuUpdateNotAllowed()
        {
            var sku = MockSku();

            var skuCatalog = MockSkuCatalog(sku);

            var environmentRepository = new MockCloudEnvironmentRepository();

            var environment = MockEnvironment(
                skuName: sku.SkuName,
                state: CloudEnvironmentState.Shutdown);

            environment = await environmentRepository.CreateAsync(environment, Logger);

            var update = new CloudEnvironmentUpdate
            {
                SkuName = "some sku",
            };

            var manager = CreateManager(environmentRepository: environmentRepository, skuCatalog: skuCatalog);

            var result = await manager.UpdateSettingsAsync(environment, update, null, Logger);

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

            var environmentRepository = new MockCloudEnvironmentRepository();

            var environment = MockEnvironment(
                skuName: activeSku.SkuName,
                state: CloudEnvironmentState.Shutdown);

            environment = await environmentRepository.CreateAsync(environment, Logger);

            var update = new CloudEnvironmentUpdate
            {
                SkuName = "bad sku name",
            };

            var manager = CreateManager(environmentRepository: environmentRepository, skuCatalog: skuCatalog);

            var result = await manager.UpdateSettingsAsync(environment, update, null, Logger);

            Assert.False(result.IsSuccess);
            Assert.Single(result.ValidationErrors);
            Assert.Equal(MessageCodes.RequestedSkuIsInvalid, result.ValidationErrors.First());
        }

        [Fact]
        public async Task RenameEnvironment()
        {
            var sku = MockSku();
            var skuCatalog = MockSkuCatalog(sku);
            var environmentRepository = new MockCloudEnvironmentRepository();
            var environment = MockEnvironment(skuName: sku.SkuName);

            environment = await environmentRepository.CreateAsync(environment, Logger);
            environment.State = CloudEnvironmentState.Shutdown;

            var update = new CloudEnvironmentUpdate
            {
                FriendlyName = "XYZ",
            };

            var manager = CreateManager(environmentRepository: environmentRepository, skuCatalog: skuCatalog);

            var result = await manager.UpdateSettingsAsync(environment, update, null, Logger);
            Assert.True(result.IsSuccess);
            Assert.Equal(update.FriendlyName, result.CloudEnvironment?.FriendlyName);
        }

        [Fact]
        public async Task RenameEnvironment_Conflict()
        {
            var sku = MockSku();
            var skuCatalog = MockSkuCatalog(sku);
            var environmentRepository = new MockCloudEnvironmentRepository();
            var environment1 = MockEnvironment(id: "env1", name: "name1", skuName: sku.SkuName);
            var environment2 = MockEnvironment(id: "env2", name: "name2", skuName: sku.SkuName);

            environment1 = await environmentRepository.CreateAsync(environment1, Logger);
            environment2 = await environmentRepository.CreateAsync(environment2, Logger);
            environment1.State = CloudEnvironmentState.Shutdown;

            var update = new CloudEnvironmentUpdate
            {
                FriendlyName = environment2.FriendlyName,
            };

            var manager = CreateManager(environmentRepository: environmentRepository, skuCatalog: skuCatalog);

            var result = await manager.UpdateSettingsAsync(environment1, update, null, Logger);
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

            var environmentRepository = new Mock<ICloudEnvironmentRepository>();
            environmentRepository
                .Setup(x => x.GetAsync(It.IsAny<DocumentDbKey>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(() => Task.FromResult(environment));

            int attempt = 0;
            environmentRepository
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

            var manager = CreateManager(environmentRepository: environmentRepository.Object, skuCatalog: skuCatalog);

            var result = await manager.UpdateSettingsAsync(environment, update, null, Logger);
            Assert.True(result.IsSuccess);
            Assert.Equal(update.FriendlyName, result.CloudEnvironment?.FriendlyName);
        }

        [Fact]
        public async Task MoveEnvironment()
        {
            var sku = MockSku();
            var skuCatalog = MockSkuCatalog(sku);
            var environmentRepository = new MockCloudEnvironmentRepository();
            var billingEventRepository = new MockBillingEventRepository();

            var plan1 = MockPlan(name: "plan1");
            var plan2 = MockPlan(name: "plan2");

            var environment = MockEnvironment(skuName: sku.SkuName, planId: plan1.ResourceId);
            var environmentOwnerId = environment.OwnerId;

            environment = await environmentRepository.CreateAsync(environment, Logger);
            environment.State = CloudEnvironmentState.Shutdown;

            var update = new CloudEnvironmentUpdate
            {
                Plan = new VsoPlan { Plan = plan2 },
            };

            var manager = CreateManager(
                environmentRepository: environmentRepository,
                skuCatalog: skuCatalog,
                billingEventRepository: billingEventRepository);

            var subscription = new Subscription
            {
                Id = plan1.Subscription,
            };
            var result = await manager.UpdateSettingsAsync(environment, update, subscription, Logger);
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
            var environmentRepository = new MockCloudEnvironmentRepository();
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
            }, Logger);

            var environment = MockEnvironment(skuName: sku.SkuName, planId: plan1.Plan.ResourceId);
            environment.OwnerId = plan1.Id + "_" + Guid.Empty;

            environment = await environmentRepository.CreateAsync(environment, Logger);
            environment.State = CloudEnvironmentState.Shutdown;

            var update = new CloudEnvironmentUpdate
            {
                Plan = plan2,
            };

            var manager = CreateManager(
                environmentRepository: environmentRepository,
                skuCatalog: skuCatalog,
                planRepository: planRepository,
                billingEventRepository: billingEventRepository);

            var subscription = new Subscription
            {
                Id = plan1.Plan.Subscription,
            };
            var result = await manager.UpdateSettingsAsync(environment, update, subscription, Logger);
            Assert.True(result.IsSuccess);
            Assert.Equal(update.Plan.Plan.ResourceId, result.CloudEnvironment?.PlanId);
            Assert.StartsWith(plan2.Id, environment.OwnerId);
        }

        [Fact]
        public async Task MoveEnvironment_Conflict()
        {
            var sku = MockSku();
            var skuCatalog = MockSkuCatalog(sku);
            var environmentRepository = new MockCloudEnvironmentRepository();
            var billingEventRepository = new MockBillingEventRepository();

            var plan1 = MockPlan(name: "plan1");
            var plan2 = MockPlan(name: "plan2");

            var environment1 = MockEnvironment(id: "env1", name: "name1", planId: plan1.ResourceId, skuName: sku.SkuName);
            var environment2 = MockEnvironment(id: "env2", name: "name2", planId: plan2.ResourceId, skuName: sku.SkuName);

            environment1 = await environmentRepository.CreateAsync(environment1, Logger);
            environment2 = await environmentRepository.CreateAsync(environment2, Logger);
            environment1.State = CloudEnvironmentState.Shutdown;

            var update = new CloudEnvironmentUpdate
            {
                FriendlyName = environment2.FriendlyName,
                Plan = new VsoPlan { Plan = plan2 },
            };

            var manager = CreateManager(
                environmentRepository: environmentRepository,
                skuCatalog: skuCatalog,
                billingEventRepository: billingEventRepository);

            var subscription = new Subscription
            {
                Id = plan1.Subscription,
            };
            var result = await manager.UpdateSettingsAsync(environment1, update, subscription, Logger);
            Assert.False(result.IsSuccess);
            Assert.Single(result.ValidationErrors);
            Assert.Equal(MessageCodes.EnvironmentNameAlreadyExists, result.ValidationErrors.Single());
        }

        [Fact]
        public async Task MoveEnvironment_ExceededQuota()
        {
            var sku = MockSku();
            var skuCatalog = MockSkuCatalog(sku);
            var environmentRepository = new MockCloudEnvironmentRepository();
            var billingEventRepository = new MockBillingEventRepository();

            var plan1 = MockPlan(name: "plan1");
            var plan2 = MockPlan(name: "plan2");

            var environment1 = MockEnvironment(id: "env1", name: "name1", planId: plan1.ResourceId, skuName: sku.SkuName);
            var environment2 = MockEnvironment(id: "env2", name: "name2", planId: plan2.ResourceId, skuName: sku.SkuName);

            environment1 = await environmentRepository.CreateAsync(environment1, Logger);
            environment2 = await environmentRepository.CreateAsync(environment2, Logger);
            environment1.State = CloudEnvironmentState.Shutdown;

            var update = new CloudEnvironmentUpdate
            {
                Plan = new VsoPlan { Plan = plan2 },
            };

            // Configure max 1 env per plan so quota will be exceeded when moving.
            var environmentSettings = new EnvironmentManagerSettings()
            {
                DefaultMaxEnvironmentsPerPlan = 1,
            };

            var manager = CreateManager(
                environmentRepository: environmentRepository,
                skuCatalog: skuCatalog,
                billingEventRepository: billingEventRepository,
                environmentSettings: environmentSettings);

            var subscription = new Subscription
            {
                Id = plan1.Subscription,
            };
            var result = await manager.UpdateSettingsAsync(environment1, update, subscription, Logger);
            Assert.False(result.IsSuccess);
            Assert.Equal(MessageCodes.ExceededQuota, result.ValidationErrors.Single());
        }

        private EnvironmentManager CreateManager(
            ICloudEnvironmentRepository environmentRepository = null,
            ISkuCatalog skuCatalog = null,
            IPlanRepository planRepository = null,
            IBillingEventRepository billingEventRepository = null,
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

            environmentRepository = environmentRepository ?? new MockCloudEnvironmentRepository();
            planRepository ??= new MockPlanRepository();
            billingEventRepository ??= new MockBillingEventRepository();
            var billingEventManager = new BillingEventManager(billingEventRepository, new MockBillingOverrideRepository());
            var workspaceRepository = new MockClientWorkspaceRepository();
            var tokenProvider = EnvironmentManagerTestsBase.MockTokenProvider();
            var resourceBroker = new MockResourceBrokerClient();
            var environmentMonitor = new MockEnvironmentMonitor();
            var metricsLogger = new MockEnvironmentMetricsLogger();
            var environmentContinuation = new Mock<IEnvironmentContinuationOperations>().Object;
            var environmentStateManager = new EnvironmentStateManager(billingEventManager, metricsLogger);
            var serviceProvider = new Mock<IServiceProvider>();
            var environmentRepairWorkflows = new List<IEnvironmentRepairWorkflow>() { new ForceSuspendEnvironmentWorkflow(environmentStateManager, resourceBroker, environmentRepository, serviceProvider.Object) };
            var resourceAllocationManager = new ResourceAllocationManager(resourceBroker);
            var workspaceManager = new WorkspaceManager(workspaceRepository);
            var subscriptionManager = new MockSubscriptionManager();
            var secretStoreManager = new Mock<ISecretStoreManager>().Object;
            var systemConfiguration = new Mock<ISystemConfiguration>().Object;

            skuCatalog = skuCatalog ?? MockSkuCatalog();
            var resourceSelector = new ResourceSelectorFactory(skuCatalog, systemConfiguration);
            var planManager = new PlanManager(planRepository, planSettings, skuCatalog);

            return new EnvironmentManager(
                environmentRepository,
                resourceBroker,
                tokenProvider,
                skuCatalog,
                environmentMonitor,
                environmentContinuation,
                environmentSettings,
                planManager,
                planSettings,
                environmentStateManager,
                environmentRepairWorkflows,
                resourceAllocationManager,
                workspaceManager,
                secretStoreManager,
                resourceSelector);
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
            AzureLocation controlPlanLocation = AzureLocation.CentralUs,
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
                ControlPlaneLocation = controlPlanLocation,
                SkuName = skuName,
                PlanId = planId ?? MockPlan().ResourceId,
                OwnerId = ownerId,
                State = state,
                AutoShutdownDelayMinutes = autoShutdownDelayMinutes
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
