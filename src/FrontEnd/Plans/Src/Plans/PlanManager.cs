// <copyright file="PlanManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans
{
    /// <inheritdoc/>
    public class PlanManager : IPlanManager
    {
        private readonly IPlanRepository planRepository;
        private readonly PlanManagerSettings planManagerSettings;

        private int cachedTotalPlansCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanManager"/> class.
        /// </summary>
        /// <param name="planRepository">Target plan repository.</param>
        /// <param name="planManagerSettings">Target plan manager settings.</param>
        public PlanManager(
            IPlanRepository planRepository,
            PlanManagerSettings planManagerSettings)
        {
            this.planRepository = planRepository;
            this.planManagerSettings = Requires.NotNull(planManagerSettings, nameof(planManagerSettings));
        }

        /// <inheritdoc/>
        public async Task<PlanManagerServiceResult> CreateAsync(VsoPlan model, IDiagnosticsLogger logger)
        {
            var result = default(PlanManagerServiceResult);

            // Validate Plan quota is not reached.
            if (await IsPlanCreationAllowedAsync(model.Plan.Subscription, logger))
            {
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

            result.VsoPlan = await this.planRepository.CreateOrUpdateAsync(model, logger);
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
            var plans = await ListAsync(userId: null, subscriptionId, resourceGroup: null, logger);

            return plans.Count() >= await planManagerSettings.MaxPlansPerSubscriptionAsync(subscriptionId, logger);
        }

        /// <inheritdoc/>
        public async Task RefreshTotalPlansCountAsync(IDiagnosticsLogger logger)
        {
            this.cachedTotalPlansCount = await this.planRepository.GetCountAsync(logger);
        }

        /// <inheritdoc/>
        public async Task<PlanManagerServiceResult> GetAsync(VsoPlanInfo plan, IDiagnosticsLogger logger, bool includeDeleted = false)
        {
            ValidationUtil.IsRequired(plan, nameof(VsoPlanInfo));

            // TODO: just return the VsoPlan and not the PlanManagerServiceResult
            // If null then PlanDoesNotExist
            VsoPlan innerPlan = (await this.planRepository.GetWhereAsync(
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
            string userId,
            string subscriptionId,
            string resourceGroup,
            IDiagnosticsLogger logger,
            bool includeDeleted = false)
        {
            // Consider pulling the IsDeleted in the getWhere calls to be conditional on includeDeleted == true
            if (userId != null)
            {
                if (resourceGroup != null)
                {
                    ValidationUtil.IsRequired(subscriptionId, nameof(subscriptionId));

                    return await this.planRepository.GetWhereAsync(
                        (model) => model.UserId == userId &&
                            model.Plan.Subscription == subscriptionId &&
                            model.Plan.ResourceGroup == resourceGroup &&
                            (model.IsDeleted == null || model.IsDeleted == false || model.IsDeleted == includeDeleted),
                        logger,
                        null);
                }
                else if (subscriptionId != null)
                {
                    return await this.planRepository.GetWhereAsync(
                        (model) => model.UserId == userId &&
                            model.Plan.Subscription == subscriptionId &&
                            (model.IsDeleted == null || model.IsDeleted == false || model.IsDeleted == includeDeleted),
                        logger,
                        null);
                }
                else
                {
                    return await this.planRepository.GetWhereAsync(
                        (model) => model.UserId == userId &&
                            (model.IsDeleted == null || model.IsDeleted == false || model.IsDeleted == includeDeleted), logger,
                        null);
                }
            }
            else
            {
                ValidationUtil.IsRequired(subscriptionId, nameof(subscriptionId));

                if (resourceGroup != null)
                {
                    return await this.planRepository.GetWhereAsync(
                        (model) => model.Plan.Subscription == subscriptionId &&
                            model.Plan.ResourceGroup == resourceGroup &&
                            (model.IsDeleted == null || model.IsDeleted == false || model.IsDeleted == includeDeleted),
                        logger,
                        null);
                }
                else
                {
                    return await this.planRepository.GetWhereAsync(
                        (model) => model.Plan.Subscription == subscriptionId &&
                            (model.IsDeleted == null || model.IsDeleted == false || model.IsDeleted == includeDeleted), logger,
                        null);
                }
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(VsoPlanInfo plan, IDiagnosticsLogger logger)
        {
            // Find model in DB
            // Location is not provided on a DELETE operation from RPSaaS,
            // thus we can only compare Name, Subscription, and ResourceGroup which should be sufficient
            var savedModel = await this.planRepository.GetWhereAsync(
                (model) => model.Plan.Name == plan.Name &&
                           model.Plan.Subscription == plan.Subscription &&
                           model.Plan.ResourceGroup == plan.ResourceGroup &&
                           model.IsDeleted != true,
                logger,
                null);
            var modelList = savedModel.ToList().SingleOrDefault();

            if (modelList == null)
            {
                // Nothing to delete, Plan does not exist
                return false;
            }

            modelList.IsDeleted = true;

            var updatedModel = await this.planRepository.UpdateAsync(modelList, logger);
            return updatedModel.IsDeleted;
        }
    }
}
