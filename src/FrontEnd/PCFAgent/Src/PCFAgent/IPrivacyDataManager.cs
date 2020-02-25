// <copyright file="IPrivacyDataManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PCFAgent
{
    /// <summary>
    /// Privacy Data Manager.
    /// </summary>
    public interface IPrivacyDataManager
    {
        /// <summary>
        /// Perform Delete.
        /// </summary>
        /// <param name="userIdSet">User Id.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>The number of entities deleted.</returns>
        Task<int> PerformDeleteAsync(UserIdSet userIdSet, IDiagnosticsLogger logger);

        /// <summary>
        /// Perform export.
        /// </summary>
        /// <param name="userIdSet">User Id.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>The number of entities and a jsonObject to be exported.</returns>
        Task<(int, JObject)> PerformExportAsync(UserIdSet userIdSet, IDiagnosticsLogger logger);
    }
}