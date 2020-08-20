// <copyright file="PlanManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.Azure.Documents.SystemFunctions;
using Microsoft.Azure.Management.CosmosDB.Fluent.Models;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans
{
    /// <inheritdoc/>
    public class PlanManager : IPlanManager
    {
        private const string LogBaseName = "plan_manager";

        private readonly IPlanRepository planRepository;
        private readonly PlanManagerSettings planManagerSettings;
        private readonly IEnumerable<string> guidChars = ScheduledTaskHelpers.GetIdShards();
        private readonly TimeSpan pagingDelay = TimeSpan.FromSeconds(1);
        private readonly ISkuCatalog skuCatalog;
        private readonly ICurrentLocationProvider currentLocationProvider;
        private int cachedTotalPlansCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanManager"/> class.
        /// </summary>
        /// <param name="planRepository">Target plan repository.</param>
        /// <param name="planManagerSettings">Target plan manager settings.</param>
        /// <param name="skuCatalog">The sku catalog.</param>
        /// <param name="currentLocationProvider">The current location provider.</param>
        public PlanManager(
            IPlanRepository planRepository,
            PlanManagerSettings planManagerSettings,
            ISkuCatalog skuCatalog,
            ICurrentLocationProvider currentLocationProvider)
        {
            this.planRepository = planRepository;
            this.planManagerSettings = Requires.NotNull(planManagerSettings, nameof(planManagerSettings));
            this.skuCatalog = Requires.NotNull(skuCatalog, nameof(skuCatalog));
            this.currentLocationProvider = Requires.NotNull(currentLocationProvider, nameof(currentLocationProvider));
        }

        /// <inheritdoc/>
        public async Task<PlanManagerServiceResult> CreateAsync(VsoPlan model, Subscription subscription, IDiagnosticsLogger logger)
        {
            var result = default(PlanManagerServiceResult);

            // Validate subscription
            if (subscription.IsBanned)
            {
                logger.LogError($"{LogBaseName}_create_bannedsubscription_error");

                result.VsoPlan = null;
                result.ErrorCode = Contracts.ErrorCodes.SubscriptionBanned;
                result.ErrorMessage = "The subscription attempting to create a plan has been disabled for this service.";
                return result;
            }

            // Validate Plan quota is not reached.
            if (!await IsPlanCreationAllowedAsync(model.Plan.ProviderNamespace, model.Plan.Subscription, logger))
            {
                logger.LogError($"{LogBaseName}_create_maxplansforsubscription_error");

                result.VsoPlan = null;
                result.ErrorCode = Contracts.ErrorCodes.ExceededQuota;
                result.ErrorMessage = "The subscription has exceeded its quota for plan creation. Please delete or remove existing plans or contact Azure support to increase the plan quota.";

                return result;
            }

            // Validate subscription state
            if (!subscription.CanCreateEnvironmentsAndPlans)
            {
                logger.LogErrorWithDetail("plan_create_error", $"Plan creation failed. Subscription is not in a Registered state.");
                result.VsoPlan = null;
                result.ErrorCode = Contracts.ErrorCodes.SubscriptionStateNotRegistered;
                result.ErrorMessage = $"The subscription must be in a registered state in order to create a plan. This subscription is currently in a {Enum.GetName(typeof(SubscriptionStateEnum), subscription.SubscriptionState)} state. Please check the state of your Azure subscription.";
                return result;
            }

            var savedModel = await GetAsync(model.Plan, logger, true);
            if (savedModel != null)
            {
                // Overwritting the original model and re-saving with the old id
                model.Id = savedModel.Id;
            }
            else
            {
                // Give the model a new ID and save it.
                model.Id = Guid.NewGuid().ToString();
            }

            // Make sure to unset isDeleted/isFinalBillSubmitted values
            model.FinalBillSubmittedDate = null;
            model.IsFinalBillSubmitted = false;
            model.DeletedDate = null;
            model.IsDeleted = false;

            result.VsoPlan = await planRepository.CreateOrUpdateAsync(model, logger);
            return result;
        }

        /// <inheritdoc/>
        public async Task<bool> CheckFeatureFlagsAsync(VsoPlan model, PlanFeatureFlag featureFlag, IDiagnosticsLogger logger)
        {
            switch (featureFlag)
            {
                case PlanFeatureFlag.VnetInjection:
                    var isVnetFeaturesEnabled = await planManagerSettings.VnetInjectionEnabledAsync(logger.NewChildLogger());
                    var isVnetPropertySet = model.Properties?.VnetProperties?.SubnetId != default;
                    return !isVnetPropertySet || isVnetFeaturesEnabled;
                default: throw new ArgumentException($"{featureFlag} is not recognized.");
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsPlanCreationAllowedAsync(string providerNamespace, string subscriptionId, IDiagnosticsLogger logger)
        {
            var plans = await ListAsync(userIdSet: null, providerNamespace, subscriptionId, resourceGroup: null, name: null, logger);

            return plans.Count() < await planManagerSettings.MaxPlansPerSubscriptionAsync(subscriptionId, logger);
        }

        /// <inheritdoc/>
        public async Task RefreshTotalPlansCountAsync(IDiagnosticsLogger logger)
        {
            cachedTotalPlansCount = await planRepository.GetCountAsync(logger);
        }

        /// <inheritdoc/>
        public async Task<VsoPlan> GetAsync(VsoPlanInfo plan, IDiagnosticsLogger logger, bool includeDeleted = false)
        {
            ValidationUtil.IsRequired(plan, nameof(VsoPlanInfo));

            return (await ListAsync(
                userIdSet: null,
                providerNamespace: plan.ProviderNamespace,
                subscriptionId: plan.Subscription,
                resourceGroup: plan.ResourceGroup,
                name: plan.Name,
                logger,
                includeDeleted)).SingleOrDefault();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<VsoPlan>> ListAsync(
            UserIdSet userIdSet,
            string providerNamespace,
            string subscriptionId,
            string resourceGroup,
            string name,
            IDiagnosticsLogger logger,
            bool includeDeleted = false)
        {
            IEnumerable<VsoPlan> plans;

            // Consider pulling the IsDeleted in the getWhere calls to be conditional on includeDeleted == true
            if (userIdSet != null)
            {
                if (name != null)
                {
                    ValidationUtil.IsRequired(subscriptionId, nameof(subscriptionId));
                    ValidationUtil.IsRequired(resourceGroup, nameof(resourceGroup));

                    plans = (await this.planRepository.GetWhereAsync(
                        (model) => (model.UserId == userIdSet.CanonicalUserId || model.UserId == userIdSet.ProfileProviderId) &&
                            model.Plan.Subscription == subscriptionId &&
                            model.Plan.ResourceGroup == resourceGroup &&
                            model.Plan.Name == name,
                        logger,
                        null))
                        .Where(x => x.IsDeleted == false || x.IsDeleted == includeDeleted);
                }
                else if (resourceGroup != null)
                {
                    ValidationUtil.IsRequired(subscriptionId, nameof(subscriptionId));

                    plans = (await this.planRepository.GetWhereAsync(
                        (model) => (model.UserId == userIdSet.CanonicalUserId || model.UserId == userIdSet.ProfileProviderId) &&
                            model.Plan.Subscription == subscriptionId &&
                            model.Plan.ResourceGroup == resourceGroup,
                        logger,
                        null))
                        .Where(x => x.IsDeleted == false || x.IsDeleted == includeDeleted);
                }
                else if (subscriptionId != null)
                {
                    plans = (await this.planRepository.GetWhereAsync(
                        (model) => (model.UserId == userIdSet.CanonicalUserId || model.UserId == userIdSet.ProfileProviderId) &&
                            model.Plan.Subscription == subscriptionId,
                        logger,
                        null))
                        .Where(x => x.IsDeleted == false || x.IsDeleted == includeDeleted);
                }
                else
                {
                    plans = (await planRepository
                        .GetWhereAsync(
                        (model) => model.UserId == userIdSet.CanonicalUserId || model.UserId == userIdSet.ProfileProviderId,
                        logger,
                        null))
                        .Where(x => x.IsDeleted == false || x.IsDeleted == includeDeleted);
                }
            }
            else
            {
                ValidationUtil.IsRequired(subscriptionId, nameof(subscriptionId));

                if (name != null)
                {
                    ValidationUtil.IsRequired(resourceGroup, nameof(resourceGroup));

                    plans = (await planRepository.GetWhereAsync(
                        (model) => model.Plan.Subscription == subscriptionId &&
                            model.Plan.ResourceGroup == resourceGroup &&
                            model.Plan.Name == name,
                        logger,
                        null))
                        .Where(x => x.IsDeleted == false || x.IsDeleted == includeDeleted);
                }
                else if (resourceGroup != null)
                {
                    plans = (await planRepository.GetWhereAsync(
                        (model) => model.Plan.Subscription == subscriptionId &&
                            model.Plan.ResourceGroup == resourceGroup,
                        logger,
                        null))
                        .Where(x => x.IsDeleted == false || x.IsDeleted == includeDeleted);
                }
                else
                {
                    plans = (await planRepository.GetWhereAsync(
                        (model) => model.Plan.Subscription == subscriptionId,
                        logger,
                        null))
                        .Where(x => x.IsDeleted == false || x.IsDeleted == includeDeleted);
                }
            }

            if (providerNamespace != null)
            {
                plans = plans.Where((p) => p.Plan.ProviderNamespace == providerNamespace);
            }

            return plans;
        }

        /// <inheritdoc/>
        public async Task<VsoPlan> DeleteAsync(VsoPlan plan, IDiagnosticsLogger logger)
        {
            Requires.NotNull(plan, nameof(plan));
            Requires.NotNull(logger, nameof(logger));

            plan.IsDeleted = true;
            plan.DeletedDate = DateTime.UtcNow;
            return await planRepository.UpdateAsync(plan, logger);
        }

        /// <inheritdoc/>
        public async Task<VsoPlan> UpdateFinalBillSubmittedAsync(VsoPlan plan, IDiagnosticsLogger logger)
        {
            Requires.NotNull(plan, nameof(plan));
            Requires.NotNull(logger, nameof(logger));

            plan.IsFinalBillSubmitted = true;
            plan.FinalBillSubmittedDate = DateTime.UtcNow;
            return await planRepository.UpdateAsync(plan, logger);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<VsoPlan>> GetPlansByShardAsync(IEnumerable<AzureLocation> locations, string planShard, IDiagnosticsLogger logger)
        {
            Requires.NotNull(locations, nameof(locations));
            Requires.Argument(locations.Any(), nameof(locations), "locations collection must not me empty.");
            Requires.NotNullOrEmpty(planShard, nameof(planShard));
            Requires.NotNull(logger, nameof(logger));

            // TODO: Change this to be streaming so that we consume less memory
            var allPlans = (await planRepository.GetWhereAsync(
                (plan) => plan.Plan.Subscription.StartsWith(planShard),
                logger,
                (_, childlogger) =>
                {
                    return Task.Delay(pagingDelay);
                })).Where(t => locations.Contains(t.Plan.Location));

            return allPlans;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<VsoPlan>> GetPartnerPlansByShardAsync(
            IEnumerable<AzureLocation> locations, string planShard, Partner partner, IDiagnosticsLogger logger)
        {
            Requires.NotNull(locations, nameof(locations));
            Requires.Argument(locations.Any(), nameof(locations), "locations collection must not me empty.");
            Requires.NotNullOrEmpty(planShard, nameof(planShard));
            Requires.NotNull(logger, nameof(logger));

            try
            {
                // TODO: Change this to be streaming so that we consume less memory
                var allPlans = (await planRepository.GetWhereAsync(
                    (plan) => plan.Plan.Subscription.StartsWith(planShard) && plan.Partner == partner,
                    logger)).Where(t => locations.Contains(t.Plan.Location));

                return allPlans;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<VsoPlan>> GetBillablePlansByShardAsync(IEnumerable<AzureLocation> locations, string planShard, IDiagnosticsLogger logger)
        {
            Requires.NotNull(locations, nameof(locations));
            Requires.Argument(locations.Any(), nameof(locations), "locations collection must not me empty.");
            Requires.NotNullOrEmpty(planShard, nameof(planShard));
            Requires.NotNull(logger, nameof(logger));

            // TODO: Change this to be streaming so that we consume less memory.
            //
            // Assuming my understanding is correct, we'd need to take advantage of C# 8's IAsyncEnumerable
            // construct combined with CosmosDB's FeedIterator API (see also: ToFeedIterator(),
            // ToStreamIterator(), or GetItemQueryStreamIterator()) so that we can asynchronously yield return
            // these plans without loading them into a potentially massive list. Then, BillingService and
            // BillingSummarySubmissionService would need to be updated to use the `async foreach (...)` syntax.
            //
            // Question: Might this cause problems by having nested SQL queries going at the same time? I recall
            // having issues with this sort of thing when using SQLite. My *guess* is that Cosmos can probably
            // handle it fine.
            var plans = (await planRepository.GetBillablePlansByShardAsync(planShard, pagingDelay, logger)).Where(plan => locations.Contains(plan.Plan.Location));

            return plans;
        }

        /// <inheritdoc/>
        public Task GetBillablePlansByShardAsync(string planShard, AzureLocation location, Func<VsoPlan, IDiagnosticsLogger, Task> itemCallback, Func<IEnumerable<VsoPlan>, IDiagnosticsLogger, Task> pageResultsCallback, IDiagnosticsLogger logger)
        {
            return planRepository.ForEachAsync(
                (plan) => plan.Id.StartsWith(planShard) && (plan.IsFinalBillSubmitted != true || !plan.IsFinalBillSubmitted.IsDefined()) && plan.Plan.Location == location,
                logger,
                itemCallback,
                pageResultsCallback);
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetShards()
        {
            // Represents all the available chars in a 16 bit GUID.
            return guidChars;
        }

        /// <inheritdoc/>
        public async Task<bool> ArePlanPropertiesValidAsync(VsoPlan vsoPlan, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;

            Requires.NotNull(vsoPlan, nameof(vsoPlan));
            Requires.NotNull(logger, nameof(logger));

            if (vsoPlan.Properties == null)
            {
                return true;
            }

            var defaultSku = vsoPlan.Properties.DefaultCodespaceSku ?? vsoPlan.Properties.DefaultEnvironmentSku;
            if (!string.IsNullOrWhiteSpace(defaultSku))
            {
                if (!skuCatalog.CloudEnvironmentSkus.TryGetValue(defaultSku, out var environmentSku))
                {
                    logger.LogErrorWithDetail("plan_property_validate_error", "Environment sku not supported.");
                    return false;
                }
            }

            if (vsoPlan.Properties.DefaultAutoSuspendDelayMinutes.HasValue &&
                vsoPlan.Properties.DefaultAutoSuspendDelayMinutes < 0)
            {
                logger.LogErrorWithDetail("plan_property_validate_error", $"{nameof(vsoPlan.Properties.DefaultAutoSuspendDelayMinutes)} value {vsoPlan.Properties.DefaultAutoSuspendDelayMinutes} not supported.");
                return false;
            }

            // Validate vnet injection
            if (!await CheckFeatureFlagsAsync(vsoPlan, PlanFeatureFlag.VnetInjection, logger))
            {
                logger.LogErrorWithDetail("plan_property_validate_error", $"Vnet property can't be added as {PlanFeatureFlag.VnetInjection} is disabled.");
                return false;
            }

            // return false if default subnet is not part of Subnets.
            if (vsoPlan.Properties.VnetProperties != default &&
            !vsoPlan.Properties.VnetProperties.SubnetId.IsValidSubnetResourceId(logger.NewChildLogger()))
            {
                logger.LogErrorWithDetail("plan_property_validate_error", $"{nameof(vsoPlan.Properties.VnetProperties.SubnetId)} value {vsoPlan.Properties.VnetProperties.SubnetId} is not valid.");
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public async Task<PlanManagerServiceResult> UpdatePlanPropertiesAsync(VsoPlan vsoPlan, IDiagnosticsLogger logger)
        {
            ValidationUtil.IsRequired(vsoPlan, nameof(VsoPlan));
            ValidationUtil.IsRequired(vsoPlan.Plan, nameof(VsoPlan.Plan));
            Requires.NotNull(logger, nameof(logger));

            var currentVsoPlan = (await planRepository.GetWhereAsync(
                (model) => model.Plan.Name == vsoPlan.Plan.Name &&
                           model.Plan.Subscription == vsoPlan.Plan.Subscription &&
                           model.Plan.ResourceGroup == vsoPlan.Plan.ResourceGroup,
                logger,
                null)).Where(x => x.IsDeleted != true).SingleOrDefault();

            if (currentVsoPlan == null)
            {
                logger.LogErrorWithDetail($"{nameof(IPlanManager)}_{nameof(IPlanManager.UpdatePlanPropertiesAsync)}", $"{vsoPlan.Plan.Name} not found.");
                return new PlanManagerServiceResult
                {
                    VsoPlan = null,
                    ErrorCode = Contracts.ErrorCodes.PlanDoesNotExist,
                };
            }

            if (vsoPlan.Properties != default)
            {
                if (currentVsoPlan.Properties == null)
                {
                    currentVsoPlan.Properties = new VsoPlanProperties();
                }

                var defaultSku = vsoPlan.Properties.DefaultCodespaceSku ?? vsoPlan.Properties.DefaultEnvironmentSku;
                if (!string.IsNullOrWhiteSpace(defaultSku))
                {
                    currentVsoPlan.Properties.DefaultCodespaceSku = defaultSku;
                }

                if (vsoPlan.Properties.DefaultAutoSuspendDelayMinutes.HasValue &&
                    vsoPlan.Properties.DefaultAutoSuspendDelayMinutes.Value > 0)
                {
                    currentVsoPlan.Properties.DefaultAutoSuspendDelayMinutes = vsoPlan.Properties.DefaultAutoSuspendDelayMinutes;
                }

                if (vsoPlan.Properties.VnetProperties != default)
                {
                    if (vsoPlan.Properties.VnetProperties.SubnetId != default)
                    {
                        currentVsoPlan.Properties.VnetProperties.SubnetId = vsoPlan.Properties.VnetProperties.SubnetId;
                    }
                }
            }

            // the managed identity is always completely overwritten when the type is changed
            string managedIdentityType = vsoPlan.ManagedIdentity?.Type;
            if (!string.IsNullOrEmpty(managedIdentityType))
            {
                if (managedIdentityType.Equals("None", StringComparison.OrdinalIgnoreCase))
                {
                    // a value of none indicates that the managed identity has been explicitly removed from the resource
                    currentVsoPlan.ManagedIdentity = null;
                }
                else
                {
                    currentVsoPlan.ManagedIdentity = vsoPlan.ManagedIdentity;
                }
            }

            // the KeyVault settings are always completely overwritten when the key source is changed
            string keySource = vsoPlan.Properties.Encryption?.KeySource;
            if (!string.IsNullOrEmpty(keySource))
            {
                if (keySource.Equals("Microsoft.Codespaces", StringComparison.OrdinalIgnoreCase))
                {
                    // a value of "Microsoft.Codespaces" indicates that the plan is being set to Microsoft-managed keys, and encryption properties are cleared
                    currentVsoPlan.Properties.Encryption = null;
                }
                else
                {
                    currentVsoPlan.Properties.Encryption = vsoPlan.Properties.Encryption;
                }
            }

            var updatedPlan = await planRepository.UpdateAsync(currentVsoPlan, logger);
            return new PlanManagerServiceResult
            {
                VsoPlan = updatedPlan,
            };
        }

        /// <inheritdoc/>
        public async Task<bool> ApplyPlanPropertiesChangesAsync(VsoPlan vsoPlan, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;

            // TODO: Needs user study to decide how to apply plan settings to existing environments.
            return true;
        }
    }
}
