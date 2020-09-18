// <copyright file="SubscriptionManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Subscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Subscriptions.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Subscriptions.Http;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions.Settings;

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
        /// <param name="environmentSubscriptionManager">The environment subscription manager.</param>
        /// <param name="subscriptionOfferManager">The subscription Offer manager.</param>
        /// <param name="jobQueueProducerFactory">The job queue producer factory.</param>
        /// <param name="rpaasHttpProvider">HttpProvider for accessing RPaas's registered subscriptions endpoint.</param>
        /// <param name="skuCatalog">the sku catalog.</param>
        /// <param name="planManager">The plan manager.</param>
        public SubscriptionManager(
            SubscriptionManagerSettings options,
            ISubscriptionRepository subscriptionRepository,
            ISystemConfiguration systemConfiguration,
            IEnvironmentSubscriptionManager environmentSubscriptionManager,
            ISubscriptionOfferManager subscriptionOfferManager,
            IJobQueueProducerFactory jobQueueProducerFactory,
            IRPaaSMetaRPHttpClient rpaasHttpProvider,
            ISkuCatalog skuCatalog,
            IPlanManager planManager)
        {
            SubscriptionRepository = Requires.NotNull(subscriptionRepository, nameof(Susbscriptions.SubscriptionRepository));
            Settings = Requires.NotNull(options, nameof(options));
            SystemConfiguration = Requires.NotNull(systemConfiguration, nameof(systemConfiguration));
            EnvironmentSubscriptionManager = Requires.NotNull(environmentSubscriptionManager, nameof(environmentSubscriptionManager));
            SubscriptionOfferManager = Requires.NotNull(subscriptionOfferManager, nameof(subscriptionOfferManager));
            JobQueueProducerFactory = Requires.NotNull(jobQueueProducerFactory, nameof(jobQueueProducerFactory));
            RPaaSHttpProvider = rpaasHttpProvider;
            PlanManager = planManager;
            skuFamilies = skuCatalog.CloudEnvironmentSkus.Select(x => x.Value.ComputeSkuFamily).Distinct();
        }

        private ISubscriptionRepository SubscriptionRepository { get; }

        private SubscriptionManagerSettings Settings { get; }

        private ISystemConfiguration SystemConfiguration { get; }

        private IEnvironmentSubscriptionManager EnvironmentSubscriptionManager { get; }

        private ISubscriptionOfferManager SubscriptionOfferManager { get; }

        private IJobQueueProducerFactory JobQueueProducerFactory { get; }

        private IRPaaSMetaRPHttpClient RPaaSHttpProvider { get; }

        private IPlanManager PlanManager { get; }

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
            return await logger.OperationScopeAsync(
              $"{LoggingBaseName}_get_subscription_async",
              async (childLogger) =>
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

                  childLogger.AddSubscriptionId(subscriptionId);
                  childLogger.AddValue("SubscriptionState", subscription.SubscriptionState.ToString());
                  childLogger.AddValue("ResourceProvider", subscription.ResourceProvider == null ? VsoPlanInfo.VsoProviderNamespace : VsoPlanInfo.CodespacesProviderNamespace);

                  // Set the current quota on the subscription.
                  subscription.CurrentMaximumQuota = await GetCurrentQuota(subscription, logger.NewChildLogger());
                  subscription.CanCreateEnvironmentsAndPlans = await CanSubscriptionCreatePlansAndEnvironmentsAsync(subscription, logger.NewChildLogger());
                  return subscription;
              }, swallowException: true);
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
        public async Task<Subscription> UpdateSubscriptionStateAsync(Subscription subscription, SubscriptionStateEnum state, string resourceProvider, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LoggingBaseName}_update_subscription_state",
                async (childLogger) =>
                {
                    if ((resourceProvider.Equals(VsoPlanInfo.CodespacesProviderNamespace) && !subscription.ResourceProvider.Equals(VsoPlanInfo.CodespacesProviderNamespace)) ||
                        (resourceProvider.Equals(VsoPlanInfo.VsoProviderNamespace) && !string.IsNullOrEmpty(subscription.ResourceProvider)))
                    {
                        // This block catches mismatches in the resource provider saved in the subscriptions table and the resource provider
                        // that initiated the subscription state update.
                        // Do not update the subscription record in the db.
                        return subscription;
                    }

                    var requiresUpdate = subscription.SubscriptionState != state;

                    childLogger.AddSubscriptionId(subscription.Id)
                    .FluentAddValue("CurrentSubscriptionState", subscription.SubscriptionState)
                    .FluentAddValue("DesiredSubscriptionState", state);

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

                    // only write to the DB if something actually changed.
                    if (requiresUpdate)
                    {
                        return await SubscriptionRepository.CreateOrUpdateAsync(subscription, logger);
                    }

                    return subscription;
                }, swallowException: true);
        }

        /// <inheritdoc />
        public async Task<bool> CanSubscriptionCreatePlansAndEnvironmentsAsync(Subscription subscription, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LoggingBaseName}_can_subscription_create",
                async (childLogger) =>
                {
                    if (await Settings.GetSubscriptionExemptFeatureFlagAsync(subscription.Id, logger))
                    {
                        return true;
                    }

                    if (!await Settings.GetSubscriptionStateCheckFeatureFlagAsync(logger))
                    {
                        return true;
                    }

                    childLogger.AddSubscriptionId(subscription.Id)
                               .FluentAddValue("CurrentSubscriptionState", subscription.SubscriptionState);

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
                    childLogger.AddSubscriptionId(subscription.Id)
                                .FluentAddValue("QuotaId", quotaId);
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
                    logger.AddSubscriptionId(subscription.Id);
                    var environments = await EnvironmentSubscriptionManager.ListBySubscriptionAsync(subscription, logger.NewChildLogger());
                    environments.ToList().ForEach(async environment => await QueueEnvironmentForSuspension(environment, logger.NewChildLogger()));
                }, swallowException: true);
        }

        private async Task ApplyDeletedRulesToResources(Subscription subscription, IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync(
                $"{LoggingBaseName}_apply_warned_deleted_rules_to_resources",
                async (childLogger) =>
                {
                    logger.AddSubscriptionId(subscription.Id);
                    var plans = await PlanManager.ListAsync(null, null, subscription.Id, null, null, childLogger.NewChildLogger());
                    foreach (var plan in plans)
                    {
                        childLogger.AddVsoPlan(plan);
                        await PlanManager.DeleteAsync(plan, childLogger.NewChildLogger());
                    }
                }, swallowException: true);
        }

        private async Task QueueEnvironmentForSuspension(CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync($"{LoggingBaseName}_queue_environment_for_suspension", async (childLogger) =>
            {
                childLogger.AddCloudEnvironment(environment);
                await SuspendEnvironmentJobHandler.ExecuteAsync(JobQueueProducerFactory, environment.Id, environment.Location, logger);
            });
        }
    }
}
