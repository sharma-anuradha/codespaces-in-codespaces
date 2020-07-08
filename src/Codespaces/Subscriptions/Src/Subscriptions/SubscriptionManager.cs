// <copyright file="SubscriptionManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bond;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Subscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Subscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Subscriptions.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Subscriptions.Http;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions.Settings;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions
{
    /// <inheritdoc/>
    [LoggingBaseName(LoggingBaseName)]
    public class SubscriptionManager : ISubscriptionManager
    {
        private const string SystemConfigurationSubscriptionBannedKey = "subscriptionmanager:is-banned";
        private const string LoggingBaseName = "subscription_manager";

        private readonly IEnumerable<string> skuFamilies;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionManager"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="subscriptionRepository">The  subscriptions repository.</param>
        /// <param name="systemConfiguration">The system configuration manager.</param>
        /// <param name="environmentManager">The environment manager.</param>
        /// <param name="subscriptionOfferManager">The subscription Offer manager.</param>
        /// <param name="crossRegionActivator">The CrossRegionContinuationTaskActivator.</param>
        /// <param name="rpaasHttpProvider">HttpProvider for accessing RPaas's registered subscriptions endpoint.</param>
        /// <param name="skuCatalog">the sku catalog.</param>
        public SubscriptionManager(
            SubscriptionManagerSettings options,
            ISubscriptionRepository subscriptionRepository,
            ISystemConfiguration systemConfiguration,
            IEnvironmentManager environmentManager,
            ISubscriptionOfferManager subscriptionOfferManager,
            ICrossRegionContinuationTaskActivator crossRegionActivator,
            IRPaaSMetaRPHttpClient rpaasHttpProvider,
            ISkuCatalog skuCatalog)
        {
            SubscriptionRepository = Requires.NotNull(subscriptionRepository, nameof(Susbscriptions.SubscriptionRepository));
            Settings = Requires.NotNull(options, nameof(options));
            SystemConfiguration = Requires.NotNull(systemConfiguration, nameof(systemConfiguration));
            EnvironmentManager = Requires.NotNull(environmentManager, nameof(environmentManager));
            SubscriptionOfferManager = Requires.NotNull(subscriptionOfferManager, nameof(subscriptionOfferManager));
            CrossRegionActivator = Requires.NotNull(crossRegionActivator, nameof(crossRegionActivator));
            RPaaSHttpProvider = rpaasHttpProvider;
            skuFamilies = skuCatalog.CloudEnvironmentSkus.Select(x => x.Value.ComputeSkuFamily).Distinct();
        }

        private ISubscriptionRepository SubscriptionRepository { get; }

        private SubscriptionManagerSettings Settings { get; }

        private ISystemConfiguration SystemConfiguration { get; }

        private IEnvironmentManager EnvironmentManager { get; }

        private ISubscriptionOfferManager SubscriptionOfferManager { get; }

        private ICrossRegionContinuationTaskActivator CrossRegionActivator { get; }

        private IRPaaSMetaRPHttpClient RPaaSHttpProvider { get; }

        /// <inheritdoc/>
        public async Task<Subscription> AddBannedSubscriptionAsync(string subscriptionId, BannedReason bannedReason, string byIdentity, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
               $"{LoggingBaseName}_add_banned_subscription",
               async (childLogger) =>
               {
                   var subscription = await GetSubscriptionAsync(subscriptionId, logger);
                   subscription.BannedReason = bannedReason;
                   subscription.BannedByIdentity = byIdentity;

                   subscription = await SubscriptionRepository.CreateOrUpdateAsync(subscription, logger);
                   return subscription;
               }, swallowException: true);
        }

        /// <inheritdoc/>
        public async Task<Subscription> GetSubscriptionAsync(string subscriptionId, IDiagnosticsLogger logger, string resourceProvider = null)
        {
            var subscription = await SubscriptionRepository.GetAsync(subscriptionId, logger.NewChildLogger());

            if (subscription == null)
            {
                // Need to create a new subscription.
                subscription = new Subscription()
                {
                    Id = subscriptionId,
                    QuotaId = "FreeTrial_2014-09-01",  // Default is the trial bucket
                    SubscriptionState = SubscriptionStateEnum.Registered,
                    ResourceProvider = resourceProvider,
                };

                var details = await GetSubscriptionDetailsFromExternalSourceAsync(subscription, logger.NewChildLogger());

                if (details != null)
                {
                    subscription.QuotaId = details.QuotaId;
                    if (Enum.TryParse(details.State, true, out SubscriptionStateEnum subscriptionStateEnum))
                    {
                        subscription.SubscriptionState = subscriptionStateEnum;
                    }
                }

                // add the subscription to our repository as it's a new subscription.
                subscription = await SubscriptionRepository.CreateOrUpdateAsync(subscription, logger.NewChildLogger());
            }

            // update subscription record if using the new RP: Microsoft.Codespaces
            if (!string.IsNullOrEmpty(resourceProvider) && subscription.ResourceProvider != VsoPlanInfo.CodespacesProviderNamespace)
            {
                subscription.ResourceProvider = VsoPlanInfo.CodespacesProviderNamespace;
                subscription = await SubscriptionRepository.CreateOrUpdateAsync(subscription, logger.NewChildLogger());
            }

            // Set the current quota on the subscription.
            subscription.CurrentMaximumQuota = await GetCurrentQuota(subscription, logger.NewChildLogger());
            subscription.CanCreateEnvironmentsAndPlans = await CanSubscriptionCreatePlansAndEnvironmentsAsync(subscription, logger.NewChildLogger());
            return subscription;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Subscription>> GetRecentBannedSubscriptionsAsync(IDiagnosticsLogger logger)
        {
            return await SubscriptionRepository.GetUnprocessedBansAsync(logger);
        }

        /// <inheritdoc />
        public async Task<Subscription> UpdatedCompletedBannedSubscriptionAsync(Subscription subscription, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LoggingBaseName}_update_subscription_state",
                async (childLogger) =>
                {
                    var currentSub = await SubscriptionRepository.GetAsync(subscription.Id, childLogger.NewChildLogger());
                    currentSub.BanComplete = true;
                    return await SubscriptionRepository.UpdateAsync(currentSub, childLogger.NewChildLogger());
                }, swallowException: true);
        }

        /// <inheritdoc />
        public async Task<Subscription> UpdateSubscriptionStateAsync(Subscription subscription, SubscriptionStateEnum state, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LoggingBaseName}_update_subscription_state",
                async (childLogger) =>
                {
                    // TODO: Only do work if state has changed
                    subscription.SubscriptionState = state;
                    subscription.SubscriptionStateUpdateDate = DateTime.UtcNow;

                    if (subscription.SubscriptionState == SubscriptionStateEnum.Suspended ||
                        subscription.SubscriptionState == SubscriptionStateEnum.Warned)
                    {
                        // Warned = Suspended
                        // - Shut down all environments
                        // - User can read resources but can not create new environments or resume old environments
                        await ApplyWarnedSuspendedRulesToResources(subscription, logger);
                    }
                    else if (subscription.SubscriptionState == SubscriptionStateEnum.Deleted)
                    {
                        // Deleted
                        // - Delete all resources
                        // - User can not performa any actions
                        await ApplyDeletedRulesToResources(subscription, logger);
                    }

                    return await SubscriptionRepository.CreateOrUpdateAsync(subscription, logger);
                }, swallowException: true);
        }

        /// <inheritdoc />
        public async Task<bool> CanSubscriptionCreatePlansAndEnvironmentsAsync(Subscription subscription, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LoggingBaseName}_can_subscription_create",
                async (childLogger) =>
                {
                    if (!await Settings.GetSubscriptionStateCheckFeatureFlagAsync(logger))
                    {
                        return true;
                    }

                    return subscription.SubscriptionState == SubscriptionStateEnum.Registered;
                }, swallowException: true);
        }

        /// <inheritdoc />
        public async Task<RPRegisteredSubscriptionsRequest> GetSubscriptionDetailsFromExternalSourceAsync(Subscription subscription, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LoggingBaseName}_get_rpaas_details",
                async (childLogger) =>
                {
                    return await RPaaSHttpProvider.GetSubscriptionDetailsAsync(subscription, childLogger);
                }, swallowException: true);
        }

        /// <inheritdoc />
        public async Task<Subscription> UpdateSubscriptionQuotaAsync(Subscription subscription, string quotaId, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LoggingBaseName}_update_quota",
                async (childLogger) =>
                {
                    var currentSub = await SubscriptionRepository.GetAsync(subscription.Id, childLogger.NewChildLogger());
                    currentSub.QuotaId = quotaId;
                    return await SubscriptionRepository.UpdateAsync(currentSub, childLogger.NewChildLogger());
                }, swallowException: true);
        }

        private async Task<IDictionary<string, int>> GetCurrentQuota(Subscription subscription, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
              $"{LoggingBaseName}_get_current_quota",
              async (childLogger) =>
              {
                  // We need to see which is the true current quota per family. The user's overriden values (if they exist or the defaults in the system).
                  var defaultQuotas = new Dictionary<string, int>();
                  foreach (var skuFamily in skuFamilies)
                  {
                      var quotaLimitForQuotaIdAndFamily = await SubscriptionOfferManager.GetComputeQuotaForOfferAsync(subscription.QuotaId, skuFamily, childLogger.NewChildLogger());
                      defaultQuotas.Add(skuFamily, quotaLimitForQuotaIdAndFamily);
                  }

                  if (subscription.MaximumComputeQuota == null)
                  {
                      return defaultQuotas;
                  }

                  var userQuotas = new Dictionary<string, int>();

                  // need to merge between what's the overriden maximum and what's default.
                  foreach (var quota in defaultQuotas)
                  {
                      if (subscription.MaximumComputeQuota.TryGetValue(quota.Key, out var value))
                      {
                          userQuotas.Add(quota.Key, value);
                      }
                      else
                      {
                          userQuotas.Add(quota.Key, quota.Value);
                      }
                  }

                  return userQuotas;
              }, swallowException: true);
        }

        private async Task ApplyWarnedSuspendedRulesToResources(Subscription subscription, IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync(
                $"{LoggingBaseName}_apply_warned_suspended_rules_to_resources",
                async (childLogger) =>
                {
                    var environments = await EnvironmentManager.ListBySubscriptionAsync(subscription, logger.NewChildLogger());
                    environments.ToList().ForEach(async environment => await QueueEnvironmentForSuspension(environment, logger.NewChildLogger()));
                }, swallowException: true);
        }

        private async Task ApplyDeletedRulesToResources(Subscription subscription, IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync(
                $"{LoggingBaseName}_apply_warned_deleted_rules_to_resources",
                async (childLogger) =>
                {
                    var environments = await EnvironmentManager.ListBySubscriptionAsync(subscription, logger.NewChildLogger());
                    environments.ToList().ForEach(async environment => await QueueEnvironmentForDeletion(environment, logger.NewChildLogger()));
                }, swallowException: true);
        }

        private async Task QueueEnvironmentForSuspension(CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync($"{LoggingBaseName}_queue_environment_for_suspension", async (childLogger) =>
            {
                childLogger.AddCloudEnvironment(environment);
                var continuationInput = new EnvironmentContinuationInput { EnvironmentId = environment.Id };

                // TODO: Create these activators through a more common pattern.
                await CrossRegionActivator.ExecuteForDataPlane(EnvironmentSuspensionContinuationHandler.DefaultQueueTarget, environment.Location, continuationInput, logger);
            });
        }

        private async Task QueueEnvironmentForDeletion(CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync($"{LoggingBaseName}_queue_environment_for_deletion", async (childLogger) =>
            {
                childLogger.AddCloudEnvironment(environment);
                var continuationInput = new EnvironmentContinuationInput { EnvironmentId = environment.Id };

                // TODO: Create these activators through a more common pattern.
                await CrossRegionActivator.ExecuteForDataPlane(EnvironmentDeletionContinuationHandler.DefaultQueueTarget, environment.Location, continuationInput, logger);
            });
        }
    }
}
