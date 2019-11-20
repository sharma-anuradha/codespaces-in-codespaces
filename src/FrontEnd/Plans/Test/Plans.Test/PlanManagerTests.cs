using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Tests
{
    public class PlanManagerTests
    {
        private readonly IPlanRepository planRepository;
        private readonly PlanManager planManager;
        private readonly IDiagnosticsLoggerFactory loggerFactory;
        private readonly IDiagnosticsLogger logger;
        private static readonly string subscription = Guid.NewGuid().ToString();

        public PlanManagerTests()
        {
            loggerFactory = new DefaultLoggerFactory();
            logger = loggerFactory.New();

            var settings = new PlanManagerSettings() { DefaultMaxPlansPerSubscription = 20, DefaultGlobalPlanLimit = 100 };

            var mockSystemConfiguration = new Mock<ISystemConfiguration>();
            mockSystemConfiguration
                .Setup(x => x.GetValueAsync<int>(It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>(), settings.DefaultMaxPlansPerSubscription))
                .Returns(Task.FromResult(settings.DefaultMaxPlansPerSubscription));

            settings.Init(mockSystemConfiguration.Object);

            this.planRepository = new MockPlanRepository();
            this.planManager = new PlanManager(this.planRepository, settings);
        }

        private VsoPlan GeneratePlan(string name, string subscriptionOption = null, string userId = null)
        {
            return new VsoPlan
            {
                Plan = new VsoPlanInfo
                {
                    Subscription = subscriptionOption ?? subscription,
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
            var savedModel = (await planManager.CreateAsync(GeneratePlan("CreatePlanTest"), logger)).VsoPlan;
            Assert.NotNull(savedModel);
            Assert.NotNull(savedModel.Id);
        }

        [Fact]
        public async Task CreatePlan_Overwrite()
        {
            var plan = GeneratePlan("CreatePlanTest");
            plan.SkuPlan.Name = "oldSku";
            var savedModel = (await planManager.CreateAsync(plan, logger)).VsoPlan;
            Assert.NotNull(savedModel);
            Assert.Equal(plan.Plan.Name, savedModel.Plan.Name);

            // Delete the old plan
            await planManager.DeleteAsync(plan.Plan, logger);

            plan = GeneratePlan("CreatePlanTest");
            plan.SkuPlan.Name = "NewSku";
            var overwrittenPlan = await planManager.CreateAsync(plan, logger);
            Assert.Equal(overwrittenPlan.VsoPlan.Plan.Name, plan.Plan.Name);
            Assert.Equal("NewSku", overwrittenPlan.VsoPlan.SkuPlan.Name);
            Assert.False(overwrittenPlan.VsoPlan.IsDeleted);
            Assert.NotEqual(savedModel.SkuPlan.Name, overwrittenPlan.VsoPlan.SkuPlan.Name);
            Assert.Equal(1, (await planManager.ListAsync(plan.UserId, plan.Plan.Subscription, plan.Plan.ResourceGroup, logger, false)).Count());
            Assert.Equal(1, (await planManager.ListAsync(plan.UserId, plan.Plan.Subscription, plan.Plan.ResourceGroup, logger, true)).Count());
        }

        [Fact]
        public async Task CreateAccountChecksPerSubscriptionQuota()
        {
            for (var i = 1; i <= 20; i++)
            {
                await planManager.CreateAsync(GeneratePlan($"CreatePlanTest-{i}"), logger);
            }

            // 21st SkuPlan should fail.
            var result = await planManager.CreateAsync(GeneratePlan("CreatePlanTest"), logger);
            Assert.Null(result.VsoPlan);
            Assert.Equal(ErrorCodes.ExceededQuota, result.ErrorCode);

            // 20 Accounts exist for given subscription.
            var listAccounts = await planManager.ListAsync(null, subscription, "myRG", logger);
            Assert.Equal(20, listAccounts.Count());

            // Delete 1 SkuPlan.
            await planManager.DeleteAsync(GeneratePlan("CreatePlanTest-1").Plan, logger);

            // User should be able to create a new SkuPlan.
            var successResult = await planManager.CreateAsync(GeneratePlan("CreatePlanTest"), logger);
            Assert.NotNull(successResult.VsoPlan);
            Assert.Equal("CreatePlanTest", successResult.VsoPlan.Plan.Name);
            Assert.Equal(ErrorCodes.Unknown, successResult.ErrorCode);
        }

        [Fact]
        public async Task GetPlan()
        {
            var original = (await planManager.CreateAsync(GeneratePlan("GetPlanTest"), logger)).VsoPlan;
            var savedModel = (await planManager.GetAsync(original.Plan, logger)).VsoPlan;
            Assert.NotNull(savedModel);
            Assert.NotNull(savedModel.Id);
            Assert.Equal(original, savedModel);
        }

        [Fact]
        public async Task GetPlanReturnsDoesNotExistIfPlanDoesNotExist()
        {
            var vsoPlan = GeneratePlan("GetPlanTest");
            var getResult = await planManager.GetAsync(vsoPlan.Plan, logger);
            Assert.Equal(ErrorCodes.PlanDoesNotExist, getResult.ErrorCode);
        }

        [Fact]
        public async Task UpdatePlan()
        {
            var original = (await planManager.CreateAsync(GeneratePlan("UpdatePlanTest"), logger)).VsoPlan;
            var savedModel = (await planManager.GetAsync(original.Plan, logger)).VsoPlan;
            savedModel.SkuPlan = new Sku { Name = "Private" };
            var updatedModel = await planManager.CreateAsync(savedModel, logger);
            Assert.Equal(savedModel, updatedModel.VsoPlan);
            Assert.Equal("Private", updatedModel.VsoPlan.SkuPlan.Name);
        }

        [Fact]
        public async Task DeletePlan()
        {
            var savedModel = (await planManager.CreateAsync(GeneratePlan("DeletePlanTest"), logger)).VsoPlan;
            var result = await planManager.DeleteAsync(savedModel.Plan, logger);
            Assert.True(result);

            var deleted = await planManager.GetAsync(savedModel.Plan, logger);
            Assert.Null(deleted.VsoPlan);
        }

        [Fact]
        public async Task DeletePlan_ButStillFindIt()
        {
            var savedModel = (await planManager.CreateAsync(GeneratePlan("DeletePlanTest"), logger)).VsoPlan;
            var result = await planManager.DeleteAsync(savedModel.Plan, logger);
            Assert.True(result);

            // If we consider deleted plans, we can still find them.
            var deleted = await planManager.GetAsync(savedModel.Plan, logger, includeDeleted: true);
            Assert.True(deleted.VsoPlan.IsDeleted);
        }

        [Fact]
        public async Task GetPlansBySubscriptionAndRG()
        {
            var model1 = GeneratePlan("Model1");
            await planManager.CreateAsync(model1, logger);
            await planManager.CreateAsync(GeneratePlan("Model2"), logger);
            await planManager.CreateAsync(GeneratePlan("Model3"), logger);

            var modelList = await planManager.ListAsync(
                userId: null, model1.Plan.Subscription, model1.Plan.ResourceGroup, logger);
            Assert.NotNull(modelList);
            Assert.IsAssignableFrom<IEnumerable>(modelList);
            Assert.All(modelList, item => Assert.Contains(model1.Plan.Subscription, model1.Plan.Subscription));
        }

        [Fact]
        public async Task GetPlansBySubscription()
        {
            var subscriptionGuid1 = Guid.NewGuid().ToString();
            var subscriptionGuid2 = Guid.NewGuid().ToString();
            var model1 = GeneratePlan("Model1", subscriptionGuid1);
            await planManager.CreateAsync(model1, logger);
            await planManager.CreateAsync(GeneratePlan("Model2", subscriptionGuid2), logger);
            await planManager.CreateAsync(GeneratePlan("Model3", subscriptionGuid2), logger);

            var modelListFirst = await planManager.ListAsync(
                userId: null, subscriptionGuid1, resourceGroup: null, logger);
            var listFirst = modelListFirst.ToList();
            Assert.NotNull(listFirst);
            Assert.Single(listFirst);

            var modelListSecond = await planManager.ListAsync(
                userId: null, subscriptionGuid2, resourceGroup: null, logger);
            var listSecond = modelListSecond.ToList();
            Assert.NotNull(listSecond);
            Assert.Equal(2, listSecond.Count());
        }


        [Fact]
        public async Task GetPlansByUser()
        {
            const string testUser1 = "test1";
            const string testUser2 = "test2";
            var subscriptionGuid1 = Guid.NewGuid().ToString();
            var subscriptionGuid2 = Guid.NewGuid().ToString();
            await planManager.CreateAsync(GeneratePlan("Model1", subscriptionGuid1, testUser1), logger);
            await planManager.CreateAsync(GeneratePlan("Model2", subscriptionGuid2, testUser2), logger);
            await planManager.CreateAsync(GeneratePlan("Model3", subscriptionGuid2, testUser2), logger);

            var modelListFirst = await planManager.ListAsync(
                userId: testUser1, subscriptionId: null, resourceGroup: null, logger);
            var listFirst = modelListFirst.ToList();
            Assert.NotNull(listFirst);
            Assert.Single(listFirst);

            var modelListSecond = await planManager.ListAsync(
                userId: testUser2, subscriptionId: null, resourceGroup: null, logger);
            var listSecond = modelListSecond.ToList();
            Assert.NotNull(listSecond);
            Assert.Equal(2, listSecond.Count());
        }

        [Fact]
        public async Task ValidateUserCanCreatePlansWithLimits()
        {
            var settings = new PlanManagerSettings() { DefaultMaxPlansPerSubscription = 20, DefaultGlobalPlanLimit = 2 };
            var mockSystemConfiguration = new Mock<ISystemConfiguration>();
            mockSystemConfiguration
                .Setup(x => x.GetValueAsync<int>(It.Is<string>(y => y == "quota:global-max-plans"), It.IsAny<IDiagnosticsLogger>(), settings.DefaultGlobalPlanLimit))
                .Returns(Task.FromResult(settings.DefaultGlobalPlanLimit));
            mockSystemConfiguration
                .Setup(x => x.GetValueAsync<int>(It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>(), settings.DefaultMaxPlansPerSubscription))
                .Returns(Task.FromResult(settings.DefaultMaxPlansPerSubscription));
            settings.Init(mockSystemConfiguration.Object);

            var planManager = new PlanManager(this.planRepository, settings);

            var whiteListedUser = new Profile() { Programs = new Dictionary<string, object> { { "vs.cloudenvironements.previewuser", true }, } };
            var normalUser = new Profile() { Programs = new Dictionary<string, object> { } };

            Assert.True(await planManager.IsPlanCreationAllowedForUserAsync(whiteListedUser, logger));
            Assert.True(await planManager.IsPlanCreationAllowedForUserAsync(normalUser, logger));

            const string testUser = "test";
            var subscriptionGuid = Guid.NewGuid().ToString();
            await planManager.CreateAsync(GeneratePlan("Model1", subscriptionGuid, testUser), logger);
            await planManager.CreateAsync(GeneratePlan("Model2", subscriptionGuid, testUser), logger);
            await planManager.CreateAsync(GeneratePlan("Model3", subscriptionGuid, testUser), logger);
            await planManager.RefreshTotalPlansCountAsync(logger);

            Assert.True(await planManager.IsPlanCreationAllowedForUserAsync(whiteListedUser, logger));
            Assert.False(await planManager.IsPlanCreationAllowedForUserAsync(normalUser, logger));
        }
    }
}
