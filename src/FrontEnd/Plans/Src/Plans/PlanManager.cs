// <copyright file="PlanManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans
{
    /// <inheritdoc/>
    public class PlanManager : IPlanManager
    {
        private const string LogBaseName = "plan_manager";

        private readonly IPlanRepository planRepository;
        private readonly PlanManagerSettings planManagerSettings;
        private readonly IEnumerable<string> guidChars = new List<string> { "a", "b", "c", "d", "e", "f", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }.Shuffle();
        private readonly TimeSpan pagingDelay = TimeSpan.FromSeconds(1);
        private readonly ISkuCatalog skuCatalog;
        private readonly ISubscriptionManager subscriptionManager;
        private int cachedTotalPlansCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanManager"/> class.
        /// </summary>
        /// <param name="planRepository">Target plan repository.</param>
        /// <param name="planManagerSettings">Target plan manager settings.</param>
        /// <param name="skuCatalog">The sku catalog.</param>
        /// <param name="subscriptionManager">The subscription manager.</param>
        public PlanManager(
            IPlanRepository planRepository,
            PlanManagerSettings planManagerSettings,
            ISkuCatalog skuCatalog,
            ISubscriptionManager subscriptionManager)
        {
            this.planRepository = planRepository;
            this.planManagerSettings = Requires.NotNull(planManagerSettings, nameof(planManagerSettings));
            this.skuCatalog = Requires.NotNull(skuCatalog, nameof(skuCatalog));
            this.subscriptionManager = Requires.NotNull(subscriptionManager, nameof(subscriptionManager));
        }

        /// <inheritdoc/>
        public async Task<PlanManagerServiceResult> CreateAsync(VsoPlan model, IDiagnosticsLogger logger)
        {
            var result = default(PlanManagerServiceResult);

            // Validate subscription
            if (await subscriptionManager.IsBannedAsync(model.Plan.Subscription, logger))
            {
                logger.LogError($"{LogBaseName}_create_bannedsubscription_error");

                result.VsoPlan = null;
                result.ErrorCode = Contracts.ErrorCodes.SubscriptionBanned;

                return result;
            }

            // Validate Plan quota is not reached.
            if (!await IsPlanCreationAllowedAsync(model.Plan.Subscription, logger))
            {
                logger.LogError($"{LogBaseName}_create_maxplansforsubscription_error");

                result.VsoPlan = null;
                result.ErrorCode = Contracts.ErrorCodes.ExceededQuota;

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
        public async Task<bool> IsPlanCreationAllowedAsync(string subscriptionId, IDiagnosticsLogger logger)
        {
            var plans = await ListAsync(userIdSet: null, subscriptionId, resourceGroup: null, name: null, logger);

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
                subscriptionId: plan.Subscription,
                resourceGroup: plan.ResourceGroup,
                name: plan.Name,
                logger,
                includeDeleted)).SingleOrDefault();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<VsoPlan>> ListAsync(
            UserIdSet userIdSet,
            string subscriptionId,
            string resourceGroup,
            string name,
            IDiagnosticsLogger logger,
            bool includeDeleted = false)
        {
            // Consider pulling the IsDeleted in the getWhere calls to be conditional on includeDeleted == true
            if (userIdSet != null)
            {
                if (name != null)
                {
                    ValidationUtil.IsRequired(subscriptionId, nameof(subscriptionId));
                    ValidationUtil.IsRequired(resourceGroup, nameof(resourceGroup));

                    return (await this.planRepository.GetWhereAsync(
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

                    return (await this.planRepository.GetWhereAsync(
                        (model) => (model.UserId == userIdSet.CanonicalUserId || model.UserId == userIdSet.ProfileProviderId) &&
                            model.Plan.Subscription == subscriptionId &&
                            model.Plan.ResourceGroup == resourceGroup,
                        logger,
                        null))
                        .Where(x => x.IsDeleted == false || x.IsDeleted == includeDeleted);
                }
                else if (subscriptionId != null)
                {
                    return (await this.planRepository.GetWhereAsync(
                        (model) => (model.UserId == userIdSet.CanonicalUserId || model.UserId == userIdSet.ProfileProviderId) &&
                            model.Plan.Subscription == subscriptionId,
                        logger,
                        null))
                        .Where(x => x.IsDeleted == false || x.IsDeleted == includeDeleted);
                }
                else
                {
                    return (await planRepository
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

                    return (await planRepository.GetWhereAsync(
                        (model) => model.Plan.Subscription == subscriptionId &&
                            model.Plan.ResourceGroup == resourceGroup &&
                            model.Plan.Name == name,
                        logger,
                        null))
                        .Where(x => x.IsDeleted == false || x.IsDeleted == includeDeleted);
                }
                else if (resourceGroup != null)
                {
                    return (await planRepository.GetWhereAsync(
                        (model) => model.Plan.Subscription == subscriptionId &&
                            model.Plan.ResourceGroup == resourceGroup,
                        logger,
                        null))
                        .Where(x => x.IsDeleted == false || x.IsDeleted == includeDeleted);
                }
                else
                {
                    return (await planRepository.GetWhereAsync(
                        (model) => model.Plan.Subscription == subscriptionId,
                        logger,
                        null))
                        .Where(x => x.IsDeleted == false || x.IsDeleted == includeDeleted);
                }
            }
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

            if (!string.IsNullOrWhiteSpace(vsoPlan.Properties.DefaultEnvironmentSku))
            {
                if (!skuCatalog.CloudEnvironmentSkus.TryGetValue(vsoPlan.Properties.DefaultEnvironmentSku, out var environmentSku))
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

                if (!string.IsNullOrWhiteSpace(vsoPlan.Properties.DefaultEnvironmentSku))
                {
                    currentVsoPlan.Properties.DefaultEnvironmentSku = vsoPlan.Properties.DefaultEnvironmentSku;
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
