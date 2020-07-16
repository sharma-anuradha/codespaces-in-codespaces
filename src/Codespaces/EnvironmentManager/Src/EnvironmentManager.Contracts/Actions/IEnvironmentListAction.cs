// <copyright file="IEnvironmentListAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment List Action.
    /// </summary>
    public interface IEnvironmentListAction : IEntityAction<ListEnvironmentActionInput, IEnumerable<CloudEnvironment>>
    {
        /// <summary>
        /// Run environment list action.
        /// </summary>
        /// <param name="planId">Target plan Id.</param>
        /// <param name="name">Target name.</param>
        /// <param name="userIdSet">The owner's user id set. Required unless plan ID is specified.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns run result from the list action.</returns>
        Task<IEnumerable<CloudEnvironment>> Run(string planId, string name, UserIdSet userIdSet, IDiagnosticsLogger logger);
    }
}
