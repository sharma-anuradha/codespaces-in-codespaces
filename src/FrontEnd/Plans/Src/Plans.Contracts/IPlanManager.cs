// <copyright file="IPlanManager.cs" company="Microsoft">
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
        Task<PlanManagerServiceResult> CreateAsync(VsoPlan model, IDiagnosticsLogger logger);

        Task<bool> IsPlanCreationAllowedForUserAsync(Profile currentUser, IDiagnosticsLogger logger);

        Task<bool> IsPlanCreationAllowedAsync(string subscriptionId, IDiagnosticsLogger logger);

        Task RefreshTotalPlansCountAsync(IDiagnosticsLogger logger);

        Task<PlanManagerServiceResult> GetAsync(VsoPlanInfo plan, IDiagnosticsLogger logger, bool includeDeleted = false);

        Task<bool> DeleteAsync(VsoPlanInfo plan, IDiagnosticsLogger logger);

        Task<IEnumerable<VsoPlan>> ListAsync(
            UserIdSet userIdSet, string subscriptionId, string resourceGroup, string name, IDiagnosticsLogger logger, bool includeDeleted = false);

        Task<IEnumerable<VsoPlan>> GetPlansByShardAsync(IEnumerable<AzureLocation> list, string planShard, IDiagnosticsLogger childlogger);

        /// <summary>
        /// Returns the SkuPlan sharding mechanism. We have currently sharing by SubscriptionId so the returned list
        /// includes all availabe chars in a 16 bit GUID.
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetShards();
    }
}
