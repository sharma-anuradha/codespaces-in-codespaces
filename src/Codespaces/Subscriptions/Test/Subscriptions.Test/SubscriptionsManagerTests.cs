using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Subscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Subscriptions.Http;
using Microsoft.VsSaaS.Services.CloudEnvironments.Subscriptions.Test;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions.Settings;
using Moq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions.Tests
{
    public class SusbscriptionManagerTests
    {
        private readonly ISubscriptionRepository subscriptionRepository;
        private readonly ISubscriptionManager subscriptionManager;
        private readonly IDiagnosticsLoggerFactory loggerFactory;
        private readonly IDiagnosticsLogger logger;
       

        public SusbscriptionManagerTests()
        {
            loggerFactory = new DefaultLoggerFactory();
            logger = loggerFactory.New();
            var settingsManager = new SubscriptionManagerSettings
            {
                BannedDaysAgo = 2,
                IsSubscriptionStateCheckEnabled = true
            };

            var mockSystemConfiguration = new Mock<ISystemConfiguration>();
            mockSystemConfiguration
                .Setup(x => x.GetValueAsync<bool>(It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>(), settingsManager.IsSubscriptionStateCheckEnabled))
                .Returns(Task.FromResult(settingsManager.IsSubscriptionStateCheckEnabled));

            settingsManager.Init(mockSystemConfiguration.Object);
            var mockEnvironmentManger = new MockEnvironmentManager();
            subscriptionRepository = new MockSubscriptionRepository();
            var mockQuotaCatalog = new Mock<IQuotaFamilyCatalog>();
            var subscriptionOfferManager = new SubscriptionOfferManager(mockSystemConfiguration.Object, mockQuotaCatalog.Object);
            var crossRegionActivator = new MockCrossRegionContinuationTaskActivator();
            var httpClient = new Mock<IRPaaSMetaRPHttpClient>();
            var skuCatalog = new Mock<ISkuCatalog>();
            var mockStandardLinux = new Mock<ICloudEnvironmentSku>();
            mockStandardLinux.Setup(sku => sku.ComputeSkuFamily).Returns("standardDSv3Family");
            var mockPremiumLinux = new Mock<ICloudEnvironmentSku>();
            mockPremiumLinux.Setup(sku => sku.ComputeSkuFamily).Returns("standardFSv2Family");

            var skus = new Dictionary<string, ICloudEnvironmentSku>
            {
                ["standardLinux"] = mockStandardLinux.Object,
                ["premiumLinux"] = mockPremiumLinux.Object,
            };
            skuCatalog.Setup(s => s.CloudEnvironmentSkus).Returns(skus);
            subscriptionManager = new SubscriptionManager(settingsManager,
                                    subscriptionRepository,
                                    mockSystemConfiguration.Object,
                                    mockEnvironmentManger,
                                    subscriptionOfferManager,
                                    crossRegionActivator, 
                                    httpClient.Object,
                                    skuCatalog.Object);
        }


        [Fact]
        public async void CanUpdateSubscriptionState()
        {
            var subscription = await AddSubscriptionsToRepoAsync(SubscriptionStateEnum.Registered);
            var savedSub = await subscriptionManager.GetSubscriptionAsync(subscription.Id, logger);
            Assert.Equal(SubscriptionStateEnum.Registered, savedSub.SubscriptionState);
            Assert.Equal(subscription.Id, savedSub.Id);

            var updatedSub = await subscriptionManager.UpdateSubscriptionStateAsync(savedSub, SubscriptionStateEnum.Unregistered, logger);
            Assert.Equal(SubscriptionStateEnum.Unregistered, updatedSub.SubscriptionState);
        }

        [Fact]
        public async void CanAddBannedSubscription()
        {
            var subscription = await AddSubscriptionsToRepoAsync();
            var bannedSub = await subscriptionManager.AddBannedSubscriptionAsync(subscription.Id, BannedReason.None, "brbenn", logger);
            Assert.Equal("brbenn", bannedSub.BannedByIdentity);
            Assert.Equal(subscription.Id, bannedSub.Id);
        }

        [Fact]
        public async void CanGetRecentBanedSubscriptionsAsync()
        {
            var bannedSub = await AddSubscriptionsToRepoAsync(isBanned: true);
            var olderBannedSub = await AddSubscriptionsToRepoAsync(isBanned: true, isBannedComplete: true);

            var bannedSubs = (await subscriptionManager.GetRecentBannedSubscriptionsAsync( logger)).ToList();
            Assert.Single(bannedSubs);
            Assert.Equal(bannedSub.Id, bannedSubs.Single().Id);

            Assert.True(bannedSub.IsBanned);
        }

        [Fact]
        public async void CanTestIfSubscriptionState()
        {
            var subscription = await AddSubscriptionsToRepoAsync();
            var subscriptionId = subscription.Id;
            var canCreate = await subscriptionManager.CanSubscriptionCreatePlansAndEnvironmentsAsync(subscription, logger);
            Assert.True(canCreate);

            // Update SubscriptionState
            await subscriptionManager.UpdateSubscriptionStateAsync(subscription, SubscriptionStateEnum.Unregistered, logger);
            var updatedSubCanCreate = await subscriptionManager.CanSubscriptionCreatePlansAndEnvironmentsAsync(subscription, logger);
            Assert.False(updatedSubCanCreate);
        }

        [Fact]
        public async void SubscriptionRecordsIsCreatedOnGet()
        {
            Assert.Empty(await subscriptionRepository.GetWhereAsync(t => t.QuotaId == "FreeTrial_2014-09-01", logger));
            var subscriptionId = Guid.NewGuid().ToString();
            var quotaId = "FreeTrial_2014-09-01";

            var subscription = await subscriptionManager.GetSubscriptionAsync(subscriptionId, logger);
            await subscriptionManager.UpdateSubscriptionQuotaAsync(subscription, quotaId, logger);
            Assert.Single(await subscriptionRepository.GetWhereAsync(t => t.QuotaId == "FreeTrial_2014-09-01", logger));
        }

        [Fact]
        public async void SubscriptionStateCheckFeatureIsAutoEnabled()
        {
            var subscription = await AddSubscriptionsToRepoAsync(SubscriptionStateEnum.Suspended, false);
            var subCanCreate = await subscriptionManager.CanSubscriptionCreatePlansAndEnvironmentsAsync(subscription, logger);
            Assert.False(subCanCreate);
        }

        [Fact]
        public async void SubscriptionIsBanComplete()
        {
            var subscription = await AddSubscriptionsToRepoAsync(SubscriptionStateEnum.Registered, false);
            await subscriptionManager.UpdatedCompletedBannedSubscriptionAsync(subscription, logger);

            Assert.True(subscription.BanComplete);
        }

        [Fact]
        public async void SubscriptionQuotaUpdated()
        {
            var subscription = await AddSubscriptionsToRepoAsync(SubscriptionStateEnum.Registered, false);
            await subscriptionManager.UpdateSubscriptionQuotaAsync(subscription,"MyQuotaId", logger);

            Assert.Equal("MyQuotaId",subscription.QuotaId);
        }

        [Fact]
        public async void SubscriptionRecordHasCodespacesRP()
        {
            var subscription = await AddSubscriptionsToRepoAsync(SubscriptionStateEnum.Registered);
            var savedSubscription = await subscriptionManager.GetSubscriptionAsync(subscription.Id, logger);
            Assert.Null(savedSubscription.ResourceProvider);

            var updatedSubscription = await subscriptionManager.GetSubscriptionAsync(subscription.Id, logger, VsoPlanInfo.CodespacesProviderNamespace);
            Assert.Equal(VsoPlanInfo.CodespacesProviderNamespace, updatedSubscription.ResourceProvider);
        }

        private async Task<Subscription> AddSubscriptionsToRepoAsync(
                                            SubscriptionStateEnum state = default, 
                                            bool isBanned = default,
                                            bool isBannedComplete = default)
        {
            var subscription = new Subscription
            {
                Id = Guid.NewGuid().ToString(),
                SubscriptionState = state,
                BannedReason = isBanned ? BannedReason.Other : default,
                BanComplete = isBannedComplete,
            };

            return await subscriptionRepository.CreateAsync(subscription, logger);
        }

    }
}
