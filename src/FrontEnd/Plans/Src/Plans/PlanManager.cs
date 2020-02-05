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
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

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
        private int cachedTotalPlansCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanManager"/> class.
        /// </summary>
        /// <param name="planRepository">Target plan repository.</param>
        /// <param name="planManagerSettings">Target plan manager settings.</param>
        /// <param name="skuCatalog">The sku catalog.</param>
        public PlanManager(
            IPlanRepository planRepository,
            PlanManagerSettings planManagerSettings,
            ISkuCatalog skuCatalog)
        {
            this.planRepository = planRepository;
            this.planManagerSettings = Requires.NotNull(planManagerSettings, nameof(planManagerSettings));
            this.skuCatalog = Requires.NotNull(skuCatalog, nameof(skuCatalog));
        }

        /// <inheritdoc/>
        public async Task<PlanManagerServiceResult> CreateAsync(VsoPlan model, IDiagnosticsLogger logger)
        {
            var result = default(PlanManagerServiceResult);

            // Validate Plan quota is not reached.
            if (!await IsPlanCreationAllowedAsync(model.Plan.Subscription, logger))
            {
                logger.LogError($"{LogBaseName}_create_maxplansforsubscription_error");

                result.VsoPlan = null;
                result.ErrorCode = Contracts.ErrorCodes.ExceededQuota;

                return result;
            }

            var savedModel = (await GetAsync(model.Plan, logger, true)).VsoPlan;
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

            result.VsoPlan = await planRepository.CreateOrUpdateAsync(model, logger);
            return result;
        }

        /// <inheritdoc/>
        public async Task<bool> IsPlanCreationAllowedForUserAsync(Profile currentUser, IDiagnosticsLogger logger)
        {
            if (currentUser.IsCloudEnvironmentsPreviewUser())
            {
                return true;
            }

            var globalPlanLimit = await planManagerSettings.GetGlobalPlanLimitAsync(logger);
            return globalPlanLimit <= 0 || cachedTotalPlansCount < globalPlanLimit;
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
        public async Task<PlanManagerServiceResult> GetAsync(VsoPlanInfo plan, IDiagnosticsLogger logger, bool includeDeleted = false)
        {
            ValidationUtil.IsRequired(plan, nameof(VsoPlanInfo));

            // TODO: just return the VsoPlan and not the PlanManagerServiceResult
            // If null then PlanDoesNotExist
            var innerPlan = (await planRepository.GetWhereAsync(
                    (model) => model.Plan == plan, logger, null)).SingleOrDefault();
            if (!includeDeleted && innerPlan?.IsDeleted == true)
            {
                innerPlan = null;
            }

            var result = new PlanManagerServiceResult
            {
                VsoPlan = innerPlan,
            };

            if (result.VsoPlan == null)
            {
                result.ErrorCode = Contracts.ErrorCodes.PlanDoesNotExist;
            }

            return result;
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
        public async Task<bool> DeleteAsync(VsoPlanInfo plan, IDiagnosticsLogger logger)
        {
            // Find model in DB
            // Location is not provided on a DELETE operation from RPSaaS,
            // thus we can only compare Name, Subscription, and ResourceGroup which should be sufficient
            var savedModel = (await planRepository.GetWhereAsync(
                (model) => model.Plan.Name == plan.Name &&
                           model.Plan.Subscription == plan.Subscription &&
                           model.Plan.ResourceGroup == plan.ResourceGroup,
                logger,
                null)).Where(x => x.IsDeleted != true);
            var modelList = savedModel.ToList().SingleOrDefault();

            if (modelList == null)
            {
                // Nothing to delete, Plan does not exist
                return false;
            }

            modelList.IsDeleted = true;

            var updatedModel = await planRepository.UpdateAsync(modelList, logger);
            return updatedModel.IsDeleted;
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
        public IEnumerable<string> GetShards()
        {
            // Represents all the available chars in a 16 bit GUID.
            return guidChars;
        }

        /// <inheritdoc/>
        public async Task<bool> ArePlanPropertiesValidAsync(VsoPlan vsoPlan, IDiagnosticsLogger logger)
        {
            Requires.NotNull(vsoPlan, nameof(vsoPlan));
            Requires.NotNull(logger, nameof(logger));

            var currentPlan = (await planRepository.GetWhereAsync(
                (model) => model.Plan.Name == vsoPlan.Plan.Name &&
                           model.Plan.Subscription == vsoPlan.Plan.Subscription &&
                           model.Plan.ResourceGroup == vsoPlan.Plan.ResourceGroup,
                logger,
                null)).Where(x => x.IsDeleted != true).SingleOrDefault();
            if (currentPlan == null || currentPlan.Plan == null)
            {
                return false;
            }

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
                vsoPlan.Properties.DefaultAutoSuspendDelayMinutes <= 0)
            {
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

            if (!string.IsNullOrWhiteSpace(vsoPlan.Properties.DefaultEnvironmentSku))
            {
                currentVsoPlan.Properties.DefaultEnvironmentSku = vsoPlan.Properties.DefaultEnvironmentSku;
            }

            if (vsoPlan.Properties.DefaultAutoSuspendDelayMinutes.HasValue &&
                vsoPlan.Properties.DefaultAutoSuspendDelayMinutes.Value > 0)
            {
                currentVsoPlan.Properties.DefaultAutoSuspendDelayMinutes = vsoPlan.Properties.DefaultAutoSuspendDelayMinutes;
            }

            var updatedPlan = await planRepository.UpdateAsync(currentVsoPlan, logger);
            return new PlanManagerServiceResult
            {
                VsoPlan = updatedPlan,
            };
        }

        /// <inheritdoc />
        public async Task<bool> ApplyPlanPropertiesChangesAsync(VsoPlan vsoPlan, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;

            // TODO: Needs user study to decide how to apply plan settings to existing environments.
            return true;
        }
    }
}
