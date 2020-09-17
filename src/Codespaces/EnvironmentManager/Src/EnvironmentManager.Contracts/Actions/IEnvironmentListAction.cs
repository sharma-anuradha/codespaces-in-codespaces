// <copyright file="IEnvironmentListAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment List Action.
    /// </summary>
    public interface IEnvironmentListAction : IEntityAction<ListEnvironmentActionInput, object, IEnumerable<CloudEnvironment>>
    {
        /// <summary>
        /// Run environment list action.
        /// </summary>
        /// <param name="planId">Target plan Id, or null to list across all plans.</param>
        /// <param name="location">Target plan location if known.</param>
        /// <param name="name">Target name, or null to list all names.</param>
        /// <param name="identity">The identity to use for plan list access, or null to use the current user identity.</param>
        /// <param name="userIdSet">The owner's user id set. Required unless plan ID is specified.</param>
        /// <param name="deletedFilter">The enum of how deleted environments should be filtered.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns run result from the list action.</returns>
        Task<IEnumerable<CloudEnvironment>> RunAsync(
            string planId,
            AzureLocation? location,
            string name,
            VsoClaimsIdentity identity,
            UserIdSet userIdSet,
            EnvironmentListType deletedFilter,
            IDiagnosticsLogger logger);
    }
}
