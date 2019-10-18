// <copyright file="IEnvironementsRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Repository
{
    /// <summary>
    /// Environements Repository accessor.
    /// </summary>
    public interface IEnvironementsRepository
    {
        /// <summary>
        /// Gets a lits of environments for the current user.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Current users environment list.</returns>
        Task<IEnumerable<CloudEnvironmentResult>> ListEnvironmentsAsync(IDiagnosticsLogger logger);

        /// <summary>
        /// Delete selected environement.
        /// </summary>
        /// <param name="id">Target id of environement to delete.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task DeleteEnvironmentAsync(Guid id, IDiagnosticsLogger logger);

        /// <summary>
        /// Provisioned target environement.
        /// </summary>
        /// <param name="accountId">Target account id.</param>
        /// <param name="environmentName">Target environment name.</param>
        /// <param name="gitRepo">Target git repo.</param>
        /// <param name="location">Target location.</param>
        /// <param name="skuName">Target sku name.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Cloud environment result.</returns>
        Task<CloudEnvironmentResult> ProvisionEnvironmentAsync(
            string accountId,
            string environmentName,
            string gitRepo,
            string location,
            string skuName,
            IDiagnosticsLogger logger);
    }
}
