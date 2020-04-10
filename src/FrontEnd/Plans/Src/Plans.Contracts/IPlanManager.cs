﻿// <copyright file="IPlanManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans
{
    /// <summary>
    /// The front-end SkuPlan Manager.
    /// </summary>
    public interface IPlanManager
    {
        /// <summary>
        /// Creates a plan.
        /// </summary>
        /// <param name="model">The vso plan model.</param>
        /// <param name="logger">The IDiagnosticsLogger.</param>
        /// <returns>The created plan or error code in case of failure.</returns>
        Task<PlanManagerServiceResult> CreateAsync(VsoPlan model, IDiagnosticsLogger logger);

        /// <summary>
        /// Checks if plan creation is allowed for the user.
        /// </summary>
        /// <param name="currentUser">The user.</param>
        /// <param name="logger">The IDiagnosticsLogger.</param>
        /// <returns>A boolean value indicating whether the plan creation is allowed..</returns>
        Task<bool> IsPlanCreationAllowedForUserAsync(Profile currentUser, IDiagnosticsLogger logger);

        /// <summary>
        /// Check if plan creation is allowed in the given subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscription id.</param>
        /// <param name="logger">The IDiagnosticsLogger.</param>
        /// <returns>A boolean value indicating whether plan creation is allowed.</returns>
        Task<bool> IsPlanCreationAllowedAsync(string subscriptionId, IDiagnosticsLogger logger);

        /// <summary>
        /// Refreshes the total plan count.
        /// </summary>
        /// <param name="logger">The IDiagnosticsLogger.</param>
        /// <returns>A task.</returns>
        Task RefreshTotalPlansCountAsync(IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the plan from the database.
        /// </summary>
        /// <remarks>
        /// The <see cref="VsoPlanInfo.Location"/> property is not considered in the lookup.
        /// </remarks>
        /// <param name="plan">The plan identity.</param>
        /// <param name="logger">The IDiagnosticsLogger.</param>
        /// <param name="includeDeleted">If deleted plans need to be included in the result.</param>
        /// <returns>The requested plan or null if not found.</returns>
        Task<VsoPlan> GetAsync(VsoPlanInfo plan, IDiagnosticsLogger logger, bool includeDeleted = false);

        /// <summary>
        /// Performs a soft delete of the plan in the database.
        /// </summary>
        /// <remarks>
        /// This only performs a soft delete by setting <see cref="VsoPlan.IsDeleted"/> to true.  It does not remove the record from the database.
        /// </remarks>
        /// <param name="plan">The plan.</param>
        /// <param name="logger">The IDiagnosticsLogger.</param>
        /// <returns>The updated plan record with <see cref="VsoPlan.IsDeleted"/> set to true.</returns>
        Task<VsoPlan> DeleteAsync(VsoPlan plan, IDiagnosticsLogger logger);

        /// <summary>
        /// Lists the available plans for the specified parameters.
        /// </summary>
        /// <param name="userIdSet">The user id.</param>
        /// <param name="subscriptionId">The subscription id.</param>
        /// <param name="resourceGroup">The resource group.</param>
        /// <param name="name">The name.</param>
        /// <param name="logger">The IDiagnosticsLogger.</param>
        /// <param name="includeDeleted">If deleted plans need to be included.</param>
        /// <returns>The list of plans.</returns>
        Task<IEnumerable<VsoPlan>> ListAsync(
            UserIdSet userIdSet, string subscriptionId, string resourceGroup, string name, IDiagnosticsLogger logger, bool includeDeleted = false);

        /// <summary>
        /// Gets the plans by shard.
        /// </summary>
        /// <param name="list">The location list.</param>
        /// <param name="planShard">The plan shard.</param>
        /// <param name="childlogger">The IDiagnosticsLogger.</param>
        /// <returns>List of plans.</returns>
        Task<IEnumerable<VsoPlan>> GetPlansByShardAsync(IEnumerable<AzureLocation> list, string planShard, IDiagnosticsLogger childlogger);

        /// <summary>
        /// Returns the SkuPlan sharding mechanism. We have currently sharing by SubscriptionId so the returned list
        /// includes all availabe chars in a 16 bit GUID.
        /// </summary>
        /// <returns>The list of shards.</returns>
        IEnumerable<string> GetShards();

        /// <summary>
        /// Checks if the plan properties are valid.
        /// </summary>
        /// <param name="vsoPlan">The vso plan.</param>
        /// <param name="logger">The IDiagnosticsLogger.</param>
        /// <returns>A boolean value indicating whether the properties are valid.</returns>
        Task<bool> ArePlanPropertiesValidAsync(VsoPlan vsoPlan, IDiagnosticsLogger logger);

        /// <summary>
        /// Updates the plan properties in the database.
        /// </summary>
        /// <param name="vsoPlan">The vso plan.</param>
        /// <param name="logger">The IDiagnosticsLogger.</param>
        /// <returns>The updated plan or error code in case of failure.</returns>
        Task<PlanManagerServiceResult> UpdatePlanPropertiesAsync(VsoPlan vsoPlan, IDiagnosticsLogger logger);

        /// <summary>
        /// Applies the plan properties changes to existing resources under the plan.
        /// </summary>
        /// <param name="vsoPlan">The vso plan.</param>
        /// <param name="logger">The IDiagnosticsLogger.</param>
        /// <returns>A boolean value indicating whether the plan properties update was successful.</returns>
        Task<bool> ApplyPlanPropertiesChangesAsync(VsoPlan vsoPlan, IDiagnosticsLogger logger);
    }
}
