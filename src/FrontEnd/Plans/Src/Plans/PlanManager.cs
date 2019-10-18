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

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans
{
    /// <inheritdoc/>
    public class PlanManager : IPlanManager
    {
        private readonly IPlanRepository planRepository;
        private PlanManagerSettings planManagerSettings;

        public PlanManager(
                    IPlanRepository planRepository,
                    PlanManagerSettings planManagerSettings)
        {
            this.planRepository = planRepository;
            this.planManagerSettings = Requires.NotNull(planManagerSettings, nameof(planManagerSettings));
        }

        /// <summary>
        /// Creates or Updates an plan.
        /// </summary>
        public async Task<PlanManagerServiceResult> CreateOrUpdateAsync(VsoPlan model, IDiagnosticsLogger logger)
        {
            var result = default(PlanManagerServiceResult);

            var savedModel = (await GetAsync(model.Plan, logger)).VsoPlan;
            if (savedModel != null)
            {
                var plan = model.Plan;
                if (savedModel.Plan?.Name != plan?.Name)
                {
                    savedModel.Plan = plan;
                }

                result.VsoPlan = await this.planRepository.CreateOrUpdateAsync(savedModel, logger);
                return result;
            }

            model.Id = Guid.NewGuid().ToString();

            // Validate Plan quota is not reached.
            var plans = await ListAsync(userId: null, model.Plan.Subscription, resourceGroup: null, logger);
            if (plans.Count() >= planManagerSettings.MaxPlansPerSubscription)
            {
                result.VsoPlan = null;
                result.ErrorCode = ErrorCodes.ExceededQuota;
                return result;
            }

            result.VsoPlan = await this.planRepository.CreateOrUpdateAsync(model, logger);

            return result;
        }

        /// <summary>
        /// Retrieves an existing plan using the provided plan info.
        /// </summary>
        public async Task<PlanManagerServiceResult> GetAsync(VsoPlanInfo plan, IDiagnosticsLogger logger)
        {
            ValidationUtil.IsRequired(plan, nameof(VsoPlanInfo));

            // TODO: just return the VsoPlan and not the PlanManagerServiceResult
            // If null then PlanDoesNotExist
            var result = new PlanManagerServiceResult
            {
                VsoPlan = (await this.planRepository.GetWhereAsync(
                                                        (model) => model.Plan == plan, logger, null))
                                                        .SingleOrDefault(),
            };

            if (result.VsoPlan == null)
            {
                result.ErrorCode = ErrorCodes.PlanDoesNotExist;
            }

            return result;
        }

        /// <summary>
        /// Retrieves an enumerable list of plans in a subscription.
        /// </summary>
        /// <param name="userId">ID of the owner of the plans to list, or null
        /// to list plans owned by any user.</param>
        /// <param name="subscriptionId">ID of the subscription containing the plans, or null
        /// to list plans across all a user's subscriptions. Required if userId is omitted.</param>
        /// <param name="resourceGroup">Optional name of the resource group containing the plans,
        /// or null to list plans across all resource groups in the subscription.</param>
        public async Task<IEnumerable<VsoPlan>> ListAsync(
            string userId,
            string subscriptionId,
            string resourceGroup,
            IDiagnosticsLogger logger)
        {
            if (userId != null)
            {
                if (resourceGroup != null)
                {
                    ValidationUtil.IsRequired(subscriptionId, nameof(subscriptionId));

                    return await this.planRepository.GetWhereAsync(
                        (model) => model.UserId == userId &&
                            model.Plan.Subscription == subscriptionId &&
                            model.Plan.ResourceGroup == resourceGroup,
                        logger,
                        null);
                }
                else if (subscriptionId != null)
                {
                    return await this.planRepository.GetWhereAsync(
                        (model) => model.UserId == userId &&
                            model.Plan.Subscription == subscriptionId,
                        logger,
                        null);
                }
                else
                {
                    return await this.planRepository.GetWhereAsync(
                        (model) => model.UserId == userId, logger, null);
                }
            }
            else
            {
                ValidationUtil.IsRequired(subscriptionId, nameof(subscriptionId));

                if (resourceGroup != null)
                {
                    return await this.planRepository.GetWhereAsync(
                        (model) => model.Plan.Subscription == subscriptionId &&
                            model.Plan.ResourceGroup == resourceGroup,
                        logger,
                        null);
                }
                else
                {
                    return await this.planRepository.GetWhereAsync(
                        (model) => model.Plan.Subscription == subscriptionId, logger, null);
                }
            }
        }

        /// <summary>
        /// Deletes an exisitng plan using the provided plan info.
        /// </summary>
        public async Task<bool> DeleteAsync(VsoPlanInfo plan, IDiagnosticsLogger logger)
        {
            // Find model in DB
            // Location is not provided on a DELETE operation from RPSaaS,
            // thus we can only compare Name, Subscription, and ResourceGroup which should be sufficient
            var savedModel = await this.planRepository.GetWhereAsync(
                (model) => model.Plan.Name == plan.Name &&
                           model.Plan.Subscription == plan.Subscription &&
                           model.Plan.ResourceGroup == plan.ResourceGroup,
                logger,
                null);
            var modelList = savedModel.ToList().SingleOrDefault();

            if (modelList == null)
            {
                // Nothing to delete, Plan does not exist
                return false;
            }

            return await this.planRepository.DeleteAsync(
                                    new DocumentDbKey(
                                        modelList.Id,
                                        new PartitionKey(modelList.Plan.Subscription)),
                                    logger);
        }
    }
}
