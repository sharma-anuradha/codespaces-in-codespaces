// <copyright file="IPrivacyDataManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PCFAgent
{
    /// <summary>
    /// Privacy Data Manager.
    /// </summary>
    public interface IPrivacyDataManager
    {
        /// <summary>
        /// Gets the user's environments.
        /// </summary>
        /// <param name="userIdSets">User Id Sets representing the user.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>True if the user has any environments.</returns>
        Task<IEnumerable<CloudEnvironment>> GetUserEnvironments(IEnumerable<UserIdSet> userIdSets, IDiagnosticsLogger logger);

        /// <summary>
        /// Delete user environments.
        /// </summary>
        /// <param name="environments">User's environments.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>The number of entities deleted.</returns>
        Task<int> DeleteEnvironmentsAsync(IEnumerable<CloudEnvironment> environments, IDiagnosticsLogger logger);

        /// <summary>
        /// Delete user identity map.
        /// </summary>
        /// <param name="userIdSets">User Id Sets representing the user.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>The number of entities deleted.</returns>
        Task<int> DeleteUserIdentityMapAsync(IEnumerable<UserIdSet> userIdSets, IDiagnosticsLogger logger);

        /// <summary>
        /// Perform export.
        /// </summary>
        /// <param name="userIdSet">User Id.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>The number of entities and a jsonObject to be exported.</returns>
        Task<(int, JObject)> PerformExportAsync(UserIdSet userIdSet, IDiagnosticsLogger logger);
    }
}