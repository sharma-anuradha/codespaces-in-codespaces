// <copyright file="IResourceRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository
{
    /// <summary>
    /// Repository that fronts access to ResourceRecords.
    /// </summary>
    public interface IResourceRepository : IDocumentDbCollection<ResourceRecord>
    {
        /// <summary>
        /// Fetch a list of pool reference codes that are ununsed.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns a list of distinct pool reference code.</returns>
        Task<IEnumerable<string>> GetPoolCodesForUnassignedAsync(IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the PoolReadyUnassigned.
        /// </summary>
        /// <param name="poolCode">Pool Reference Code.</param>
        /// <param name="logger">Logger to be used.</param>
        /// <returns>A Resource record from the resources collection.<see cref="Task"/> representing the asynchronous operation.</returns>
        Task<ResourceRecord> GetPoolReadyUnassignedAsync(string poolCode, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the pool unassigned.
        /// </summary>
        /// <param name="poolCode">Pool Reference Code.</param>
        /// <param name="count">The count.</param>
        /// <param name="logger">Logger to be used.</param>
        /// <returns>Unassigned pool<see cref="Task"/> representing the asynchronous operation.</returns>
        Task<IEnumerable<string>> GetPoolUnassignedAsync(string poolCode, int count, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the count of the pool unassigned.
        /// </summary>
        /// <param name="poolCode">Pool Reference Code.</param>
        /// <param name="logger">Logger to be used.</param>
        /// <returns>Unassigned count<see cref="Task"/> representing the asynchronous operation.</returns>
        Task<int> GetPoolUnassignedCountAsync(string poolCode, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the count of the pool ready unassigned.
        /// </summary>
        /// <param name="poolCode">Pool Reference Code.</param>
        /// <param name="logger">Logger to be used.</param>
        /// <returns>Unassigned count<see cref="Task"/> representing the asynchronous operation.</returns>
        Task<int> GetPoolReadyUnassignedCountAsync(string poolCode, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the count of the pool unassigned version.
        /// </summary>
        /// <param name="poolCode">Pool Reference Code.</param>
        /// <param name="poolVersionCode">Pool Version Code.</param>
        /// <param name="logger">Logger to be used.</param>
        /// <returns>Unassigned count<see cref="Task"/> representing the asynchronous operation.</returns>
        Task<int> GetPoolUnassignedVersionCountAsync(string poolCode, string poolVersionCode, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the count of the pool ready unassigned version.
        /// </summary>
        /// <param name="poolCode">Pool Reference Code.</param>
        /// <param name="poolVersionCode">Pool Version Code.</param>
        /// <param name="logger">Logger to be used.</param>
        /// <returns>Unassigned count<see cref="Task"/> representing the asynchronous operation.</returns>
        Task<int> GetPoolReadyUnassignedVersionCountAsync(string poolCode, string poolVersionCode, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the count of the pool unassigned not version.
        /// </summary>
        /// <param name="poolCode">Pool Reference Code.</param>
        /// <param name="poolVersionCode">Pool Version Code.</param>
        /// <param name="logger">Logger to be used.</param>
        /// <returns>Unassigned count<see cref="Task"/> representing the asynchronous operation.</returns>
        Task<int> GetPoolUnassignedNotVersionCountAsync(string poolCode, string poolVersionCode, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the count of the pool ready unassigned not version.
        /// </summary>
        /// <param name="poolCode">Pool Reference Code.</param>
        /// <param name="poolVersionCode">Pool Version Code.</param>
        /// <param name="logger">Logger to be used.</param>
        /// <returns>Unassigned count<see cref="Task"/> representing the asynchronous operation.</returns>
        Task<int> GetPoolReadyUnassignedNotVersionCountAsync(string poolCode, string poolVersionCode, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the pool unassigned not version.
        /// </summary>
        /// <param name="poolCode">Pool Reference Code.</param>
        /// <param name="poolVersionCode">Pool Version Code.</param>
        /// <param name="count">The count.</param>
        /// <param name="logger">Logger to be used.</param>
        /// <returns>Unassigned pool not version<see cref="Task"/> representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Fetch pool queue record.
        /// </summary>
        /// <param name="poolCode">pool code.</param>
        /// <param name="logger">logger.</param>
        /// <returns>pool queue record.</returns>
        Task<ResourceRecord> GetPoolQueueRecordAsync(string poolCode, IDiagnosticsLogger logger);

        /// <summary>
        /// Get resource count by dimensions.
        /// </summary>
        /// <param name="logger">The diganostics logger.</param>
        /// <returns>Query results.</returns>
        Task<IEnumerable<SystemResourceCountByDimensions>> GetResourceCountByDimensionsAsync(IDiagnosticsLogger logger);

        /// <summary>
        /// Get resource component count by dimensions.
        /// </summary>
        /// <param name="logger">The diganostics logger.</param>
        /// <returns>Query results.</returns>
        Task<IEnumerable<SystemResourceCountByDimensions>> GetComponentCountByDimensionsAsync(IDiagnosticsLogger logger);
    }
}
