// <copyright file="IPlanManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans
{
    /// <summary>
    /// The front-end SkuPlan Manager.
    /// </summary>
    public interface IPlanManager
    {
        Task<PlanManagerServiceResult> CreateOrUpdateAsync(VsoPlan model, IDiagnosticsLogger logger);

        Task<bool> IsPlanCreationAllowedForUserAsync(Profile currentUser, IDiagnosticsLogger logger);

        Task<bool> IsPlanCreationAllowedAsync(string subscriptionId, IDiagnosticsLogger logger);

        Task RefreshTotalPlansCountAsync(IDiagnosticsLogger logger);

        Task<PlanManagerServiceResult> GetAsync(VsoPlanInfo plan, IDiagnosticsLogger logger);

        Task<bool> DeleteAsync(VsoPlanInfo plan, IDiagnosticsLogger logger);

        Task<IEnumerable<VsoPlan>> ListAsync(
            string userId, string subscriptionId, string resourceGroup, IDiagnosticsLogger logger);
    }
}
