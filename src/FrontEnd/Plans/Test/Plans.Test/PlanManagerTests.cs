using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Tests
{
    public class PlanManagerTests
    {
        private readonly IPlanRepository planRepository;
        private readonly PlanManager planManager;
        private readonly ISkuCatalog skuCatalog;
        private readonly ISubscriptionManager subscriptionManager;
        private readonly IDiagnosticsLoggerFactory loggerFactory;
        private readonly IDiagnosticsLogger logger;
        private static readonly string subscriptionId = Guid.NewGuid().ToString();
        private readonly decimal standardLinuxComputeUnitPerHr = 125;
        private readonly decimal premiumLinuxComputeUnitPerHr = 242;
        private readonly decimal standardLinuxStorageUnitPerHr = 2;
        private readonly decimal premiumLinuxStorageUnitPerHr = 3;
        private static readonly string standardLinuxSkuName = "standardLinuxSku";
        private static readonly string premiumLinuxSkuName = "premiumLinuxSku";

        public PlanManagerTests()
        {
            loggerFactory = new DefaultLoggerFactory();
            logger = loggerFactory.New();

            var settings = new PlanManagerSettings() { DefaultMaxPlansPerSubscription = 20, DefaultGlobalPlanLimit = 100, DefaultVnetInjectionEnabled = true };

            var mockSystemConfiguration = new Mock<ISystemConfiguration>();
            mockSystemConfiguration
                .Setup(x => x.GetValueAsync<int>(It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>(), settings.DefaultMaxPlansPerSubscription))
                .Returns(Task.FromResult(settings.DefaultMaxPlansPerSubscription));

            mockSystemConfiguration
               .Setup(x => x.GetValueAsync<bool>(It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>(), settings.DefaultVnetInjectionEnabled))
               .Returns(Task.FromResult(settings.DefaultVnetInjectionEnabled));

            settings.Init(mockSystemConfiguration.Object);

            skuCatalog = GetMockSKuCatalog().Object;

            planRepository = new MockPlanRepository();
            subscriptionManager = new MockSubscriptionManager();
            planManager = new PlanManager(planRepository, settings, skuCatalog);
        }

        private Mock<ISkuCatalog> GetMockSKuCatalog()
        {
            var mockStandardLinux = new Mock<ICloudEnvironmentSku>();
            mockStandardLinux.Setup(sku => sku.ComputeVsoUnitsPerHour).Returns(standardLinuxComputeUnitPerHr);
            mockStandardLinux.Setup(sku => sku.StorageVsoUnitsPerHour).Returns(standardLinuxStorageUnitPerHr);

            var mockPremiumLinux = new Mock<ICloudEnvironmentSku>();
            mockPremiumLinux.Setup(sku => sku.ComputeVsoUnitsPerHour).Returns(premiumLinuxComputeUnitPerHr);
            mockPremiumLinux.Setup(sku => sku.StorageVsoUnitsPerHour).Returns(premiumLinuxStorageUnitPerHr);

            var skus = new Dictionary<string, ICloudEnvironmentSku>
            {
                [standardLinuxSkuName] = mockStandardLinux.Object,
                [premiumLinuxSkuName] = mockPremiumLinux.Object,
            };
            var mockSkuCatelog = new Mock<ISkuCatalog>();
            mockSkuCatelog.Setup(cat => cat.CloudEnvironmentSkus).Returns(skus);
            return mockSkuCatelog;
        }

        private VsoPlan GeneratePlan(string name, string subscriptionOption = null, string userId = null)
        {
            return new VsoPlan
            {
                Plan = new VsoPlanInfo
                {
                    Subscription = subscriptionOption ?? subscriptionId,
                    ResourceGroup = "myRG",
                    Name = name,
                    Location = AzureLocation.WestUs2,
                },
                SkuPlan = new Sku
                {
                    Name = "Preview"
                },
                UserId = userId,
            };
        }

        [Fact]
        public async Task CreatePlan()
        {
            var subscription = new Subscription();
            var savedModel = (await planManager.CreateAsync(GeneratePlan("CreatePlanTest"), subscription, logger)).VsoPlan;
            Assert.NotNull(savedModel);
            Assert.NotNull(savedModel.Id);
        }

        [Fact]
        public async Task CreatePlan_Overwrite()
        {
            var plan = GeneratePlan("CreatePlanTest");
            plan.SkuPlan.Name = "oldSku";
            var subscription = new Subscription();
            var savedModel = (await planManager.CreateAsync(plan, subscription, logger)).VsoPlan;
            Assert.NotNull(savedModel);
            Assert.Equal(plan.Plan.Name, savedModel.Plan.Name);

            // Delete the old plan
            await planManager.DeleteAsync(plan, logger);

            plan = GeneratePlan("CreatePlanTest");
            plan.SkuPlan.Name = "NewSku";
            var overwrittenPlan = await planManager.CreateAsync(plan, subscription, logger);
            Assert.Equal(overwrittenPlan.VsoPlan.Plan.Name, plan.Plan.Name);
            Assert.Equal("NewSku", overwrittenPlan.VsoPlan.SkuPlan.Name);
            Assert.False(overwrittenPlan.VsoPlan.IsDeleted);
            Assert.NotEqual(savedModel.SkuPlan.Name, overwrittenPlan.VsoPlan.SkuPlan.Name);
            Assert.Single((await planManager.ListAsync(new UserIdSet(plan.UserId), plan.Plan.Subscription, plan.Plan.ResourceGroup, null, logger, false)));
            Assert.Single((await planManager.ListAsync(new UserIdSet(plan.UserId), plan.Plan.Subscription, plan.Plan.ResourceGroup, null, logger, true)));
        }

        [Fact]
        public async Task CreateAccountChecksPerSubscriptionQuota()
        {
            var subscription = new Subscription();
            VsoPlan toDelete = null;
            PlanManagerServiceResult result;
            for (var i = 1; i <= 20; i++)
            {
                result = await planManager.CreateAsync(GeneratePlan($"CreatePlanTest-{i}"), subscription, logger);
                if (i == 1)
                {
                    toDelete = result.VsoPlan;
                }
            }

            // 21st SkuPlan should fail.
            result = await planManager.CreateAsync(GeneratePlan("CreatePlanTest"), subscription, logger);
            Assert.Null(result.VsoPlan);
            Assert.Equal(ErrorCodes.ExceededQuota, result.ErrorCode);

            // 20 Accounts exist for given subscription.
            var listAccounts = await planManager.ListAsync(null, subscriptionId, "myRG", null, logger);
            Assert.Equal(20, listAccounts.Count());

            // Delete 1 SkuPlan.
            await planManager.DeleteAsync(toDelete, logger);

            // User should be able to create a new SkuPlan.
            var successResult = await planManager.CreateAsync(GeneratePlan("CreatePlanTest"), subscription, logger);
            Assert.NotNull(successResult.VsoPlan);
            Assert.Equal("CreatePlanTest", successResult.VsoPlan.Plan.Name);
            Assert.Equal(ErrorCodes.Unknown, successResult.ErrorCode);
        }

        [Fact]
        public async Task GetPlan()
        {
            var subscription = new Subscription();
            var original = (await planManager.CreateAsync(GeneratePlan("GetPlanTest"), subscription, logger)).VsoPlan;
            var savedModel = await planManager.GetAsync(original.Plan, logger);
            Assert.NotNull(savedModel);
            Assert.NotNull(savedModel.Id);
            Assert.Equal(original, savedModel);
        }

        [Fact]
        public async Task GetPlanWithoutLocationProperty()
        {
            var subscription = new Subscription();
            var plan = (await planManager.CreateAsync(GeneratePlan("GetPlanTest"), subscription, logger)).VsoPlan;

            var lookupPlan = GeneratePlan("GetPlanTest");
            lookupPlan.Plan.Location = default;

            var savedModel = await planManager.GetAsync(lookupPlan.Plan, logger);
            Assert.NotNull(savedModel);
            Assert.NotNull(savedModel.Id);
            Assert.Equal(plan.Plan.ResourceId, savedModel.Plan.ResourceId);
        }

        [Fact]
        public async Task GetPlanReturnsDoesNotExistIfPlanDoesNotExist()
        {
            var vsoPlan = GeneratePlan("GetPlanTest");
            var getResult = await planManager.GetAsync(vsoPlan.Plan, logger);
            Assert.Null(getResult);
        }

        [Fact]
        public async Task UpdatePlan()
        {
            var subscription = new Subscription();
            var original = (await planManager.CreateAsync(GeneratePlan("UpdatePlanTest"), subscription, logger)).VsoPlan;
            var savedModel = await planManager.GetAsync(original.Plan, logger);
            savedModel.SkuPlan = new Sku { Name = "Private" };
            var updatedModel = await planManager.CreateAsync(savedModel, subscription, logger);
            Assert.Equal(savedModel, updatedModel.VsoPlan);
            Assert.Equal("Private", updatedModel.VsoPlan.SkuPlan.Name);
        }

        [Fact]
        public async Task DeletePlan()
        {
            var subscription = new Subscription();
            var savedModel = (await planManager.CreateAsync(GeneratePlan("DeletePlanTest"), subscription, logger)).VsoPlan;
            var result = await planManager.DeleteAsync(savedModel, logger);
            Assert.True(result.IsDeleted);

            var deleted = await planManager.GetAsync(savedModel.Plan, logger);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task DeletePlan_ButStillFindIt()
        {
            var subscription = new Subscription();
            var savedModel = (await planManager.CreateAsync(GeneratePlan("DeletePlanTest"), subscription, logger)).VsoPlan;
            var result = await planManager.DeleteAsync(savedModel, logger);
            Assert.True(result.IsDeleted);

            // If we consider deleted plans, we can still find them.
            var deleted = await planManager.GetAsync(savedModel.Plan, logger, includeDeleted: true);
            Assert.NotNull(deleted);
            Assert.True(deleted.IsDeleted);
        }

        [Fact]
        public async Task GetPlansBySubscriptionAndRGAndName()
        {
            var model1 = GeneratePlan("Model1");
            var subscription = new Subscription();
            await planManager.CreateAsync(model1, subscription, logger);
            await planManager.CreateAsync(GeneratePlan("Model2"), subscription, logger);
            await planManager.CreateAsync(GeneratePlan("Model3"), subscription, logger);

            var modelList = await planManager.ListAsync(
                userIdSet: null, model1.Plan.Subscription, model1.Plan.ResourceGroup, model1.Plan.Name, logger);
            Assert.NotNull(modelList);
            Assert.IsAssignableFrom<IEnumerable>(modelList);
            Assert.All(modelList, item => Assert.Contains(model1.Plan.Subscription, item.Plan.Subscription));
        }

        [Fact]
        public async Task GetPlansBySubscriptionAndRG()
        {
            var model1 = GeneratePlan("Model1");
            var subscription = new Subscription();
            await planManager.CreateAsync(model1, subscription, logger);
            await planManager.CreateAsync(GeneratePlan("Model2"), subscription, logger);
            await planManager.CreateAsync(GeneratePlan("Model3"), subscription, logger);

            var modelList = await planManager.ListAsync(
                userIdSet: null, model1.Plan.Subscription, model1.Plan.ResourceGroup, null, logger);
            Assert.NotNull(modelList);
            Assert.IsAssignableFrom<IEnumerable>(modelList);
            Assert.All(modelList, item => Assert.Contains(model1.Plan.Subscription, item.Plan.Subscription));
        }

        [Fact]
        public async Task GetPlansBySubscription()
        {
            var subscriptionGuid1 = Guid.NewGuid().ToString();
            var subscriptionGuid2 = Guid.NewGuid().ToString();
            var model1 = GeneratePlan("Model1", subscriptionGuid1);
            var subscription = new Subscription();
            await planManager.CreateAsync(model1, subscription, logger);
            await planManager.CreateAsync(GeneratePlan("Model2", subscriptionGuid2), subscription, logger);
            await planManager.CreateAsync(GeneratePlan("Model3", subscriptionGuid2), subscription, logger);

            var modelListFirst = await planManager.ListAsync(
                userIdSet: null, subscriptionGuid1, resourceGroup: null, name: null, logger);
            var listFirst = modelListFirst.ToList();
            Assert.NotNull(listFirst);
            Assert.Single(listFirst);

            var modelListSecond = await planManager.ListAsync(
                userIdSet: null, subscriptionGuid2, resourceGroup: null, name: null, logger);
            var listSecond = modelListSecond.ToList();
            Assert.NotNull(listSecond);
            Assert.Equal(2, listSecond.Count());
        }

        [Fact]
        public async Task GetPlansByUser()
        {
            const string testUser1 = "test1";
            const string testUser2 = "test2";
            var testUserSet1 = new UserIdSet(testUser1);
            var testUserSet2 = new UserIdSet(testUser2);
            var subscriptionGuid1 = Guid.NewGuid().ToString();
            var subscriptionGuid2 = Guid.NewGuid().ToString();
            var subscription = new Subscription();
            await planManager.CreateAsync(GeneratePlan("Model1", subscriptionGuid1, testUser1), subscription, logger);
            await planManager.CreateAsync(GeneratePlan("Model2", subscriptionGuid2, testUser2), subscription, logger);
            await planManager.CreateAsync(GeneratePlan("Model3", subscriptionGuid2, testUser2), subscription, logger);

            var modelListFirst = await planManager.ListAsync(
                userIdSet: testUserSet1, subscriptionId: null, resourceGroup: null, name: null, logger);
            var listFirst = modelListFirst.ToList();
            Assert.NotNull(listFirst);
            Assert.Single(listFirst);

            var modelListSecond = await planManager.ListAsync(
                userIdSet: testUserSet2, subscriptionId: null, resourceGroup: null, name: null, logger);
            var listSecond = modelListSecond.ToList();
            Assert.NotNull(listSecond);
            Assert.Equal(2, listSecond.Count());
        }

        [Fact]
        public async Task PlanProperties_PatchTest()
        {
            var subscription = new Subscription();
            var createResult = await planManager.CreateAsync(new VsoPlan
            {
                Plan = new VsoPlanInfo
                {
                    Location = AzureLocation.WestUs2,
                    Name = nameof(PlanProperties_PatchTest),
                    ResourceGroup = "resourceGroup",
                    Subscription = "subscription",
                }
            }, subscription, logger);

            Assert.NotNull(createResult.VsoPlan);

            var vsoPlan = createResult.VsoPlan;

            // Null properties are valid.
            Assert.True(await planManager.ArePlanPropertiesValidAsync(vsoPlan, logger));

            var planProperties = new VsoPlanProperties
            {
                DefaultAutoSuspendDelayMinutes = 15,
                DefaultEnvironmentSku = "unknownsku"
            };

            // Unknown sku names are invalid.
            vsoPlan.Properties = planProperties;
            Assert.False(await planManager.ArePlanPropertiesValidAsync(vsoPlan, logger));

            // Known sku names are valid.
            vsoPlan.Properties.DefaultEnvironmentSku = premiumLinuxSkuName;
            Assert.True(await planManager.ArePlanPropertiesValidAsync(vsoPlan, logger));

            Assert.True(await planManager.ApplyPlanPropertiesChangesAsync(vsoPlan, logger));

            // Update succeeds.
            var updateResult = await planManager.UpdatePlanPropertiesAsync(vsoPlan, logger);
            Assert.NotNull(updateResult.VsoPlan);
            Assert.Equal(planProperties, updateResult.VsoPlan.Properties);

            // Updating a deleted plan fails.
            vsoPlan = await planManager.DeleteAsync(vsoPlan, logger);
            Assert.True(vsoPlan.IsDeleted);
            var deletedUpdateResult = await planManager.UpdatePlanPropertiesAsync(vsoPlan, logger);
            Assert.Null(deletedUpdateResult.VsoPlan);
            Assert.Equal(ErrorCodes.PlanDoesNotExist, deletedUpdateResult.ErrorCode);
        }

        [Fact]
        public async Task PlanProperties_PatchVnetPropertyTest()
        {
            var subscription = new Subscription();

            var createResult = await planManager.CreateAsync(new VsoPlan
            {
                Plan = new VsoPlanInfo
                {
                    Location = AzureLocation.WestUs2,
                    Name = nameof(PlanProperties_PatchTest),
                    ResourceGroup = "resourceGroup",
                    Subscription = "subscription",
                }
            }, subscription, logger);

            Assert.NotNull(createResult.VsoPlan);

            var vsoPlan = createResult.VsoPlan;

            var planProperties = new VsoPlanProperties
            {
                DefaultAutoSuspendDelayMinutes = 15,
                DefaultEnvironmentSku = premiumLinuxSkuName,
            };

            // Null properties are valid.
            vsoPlan.Properties = planProperties;
            Assert.True(await planManager.ArePlanPropertiesValidAsync(vsoPlan, logger));

            // Invalid Resource Id added to subnet
            vsoPlan.Properties.VnetProperties = new VsoVnetProperties();
            vsoPlan.Properties.VnetProperties.SubnetId = "invalid";
            Assert.False(await planManager.ArePlanPropertiesValidAsync(vsoPlan, logger));

            // Valid Vnet Properties
            var subnetResourceId = $"/subscriptions/{Guid.Empty}/resourceGroups/VSEng/providers/Microsoft.Network/virtualNetworks/VSEng-vnet/subnets/default";
            vsoPlan.Properties.VnetProperties.SubnetId = subnetResourceId;
            Assert.True(await planManager.ArePlanPropertiesValidAsync(vsoPlan, logger));

            Assert.True(await planManager.ApplyPlanPropertiesChangesAsync(vsoPlan, logger));

            // Update succeeds.
            var updateResult = await planManager.UpdatePlanPropertiesAsync(vsoPlan, logger);
            Assert.NotNull(updateResult.VsoPlan);
            Assert.Equal(planProperties, updateResult.VsoPlan.Properties);
        }
    }
}
