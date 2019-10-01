// <copyright file="IResourceRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository
{
    /// <summary>
    /// Repository that fronts access to ResourceRecords.
    /// </summary>
    public interface IResourceRepository : IDocumentDbCollection<ResourceRecord>
    {
        Task<ResourceRecord> GetPoolReadyUnassignedAsync(string poolCode, IDiagnosticsLogger logger);

        Task<int> GetPoolUnassignedCountAsync(string poolCode, IDiagnosticsLogger logger);

        Task<int> GetPoolReadyUnassignedCountAsync(string poolCode, IDiagnosticsLogger logger);

        Task<int> GetPoolUnassignedVersionCountAsync(string poolCode, string poolVersionCode, IDiagnosticsLogger logger);

        Task<int> GetPoolReadyUnassignedVersionCountAsync(string poolCode, string poolVersionCode, IDiagnosticsLogger logger);

        Task<int> GetPoolUnassignedNotVersionCountAsync(string poolCode, string poolVersionCode, IDiagnosticsLogger logger);

        Task<int> GetPoolReadyUnassignedNotVersionCountAsync(string poolCode, string poolVersionCode, IDiagnosticsLogger logger);

        Task<IEnumerable<string>> GetPoolUnassignedAsync(string poolCode, int count, IDiagnosticsLogger logger);

        Task<IEnumerable<string>> GetPoolUnassignedNotVersionAsync(string poolCode, string poolVersionCode, int count, IDiagnosticsLogger logger);

        /// <summary>
        /// Fetch a list of resources that have failed. Specifically those that are in a Failed or Cancelled
        /// state, or those that are considered to be stuck in Init or In Progress.
        /// </summary>
        /// <param name="poolCode">Pool Code.</param>
        /// <param name="count">Count of how many records we want to fetch.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns a list of failed resource id.</returns>
        Task<IEnumerable<ResourceRecord>> GetFailedOperationAsync(string poolCode, int count, IDiagnosticsLogger logger);
    }
}
