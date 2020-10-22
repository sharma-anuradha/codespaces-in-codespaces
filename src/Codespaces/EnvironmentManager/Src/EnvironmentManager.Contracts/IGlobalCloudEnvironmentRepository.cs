// <copyright file="IGlobalCloudEnvironmentRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// A repository of <see cref="CloudEnvironment"/>.
    /// </summary>
    public interface IGlobalCloudEnvironmentRepository : IDocumentDbCollection<CloudEnvironment>
    {
        /// <summary>
        /// Gets the control-plane location of the repository.
        /// </summary>
        AzureLocation ControlPlaneLocation { get; }

        /// <summary>
        /// Gets the count of unique subscriptions that have Environments in the cloud environments table.
        /// </summary>
        /// <param name="logger">the logger.</param>
        /// <returns>The number of Unique subscriptions in the cloud environments table.</returns>
        Task<int> GetCloudEnvironmentSubscriptionCountAsync(
            IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the count of unique plans that have Environments in the cloud environments table.
        /// </summary>
        /// <param name="logger">the logger.</param>
        /// <returns>The number of Unique plans in the cloud environments table.</returns>
        Task<int> GetCloudEnvironmentPlanCountAsync(
            IDiagnosticsLogger logger);

        /// <summary>
        /// Fetch a list of environments that have failed. Specifically those that are in a Failed or Cancelled
        /// state, or those that are considered to be stuck in Init or In Progress.
        /// </summary>
        /// <param name="idShard">Pool Code.</param>
        /// <param name="count">Limit count.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns a list of failed environments.</returns>
        Task<IEnumerable<CloudEnvironment>> GetFailedOperationAsync(
            string idShard,
            int count,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Fetch a list of environments that have are ready for archiving. Filter to the target
        /// environments. Specifically where:
        ///     Shard matches target, that we have storage, machine is shutdown and not already
        ///     archived, that the time from last status update is past our cut off and we aren't
        ///     already doing anything archive related with this environment.
        /// </summary>
        /// <param name="idShard">Pool Code.</param>
        /// <param name="count">Limit count.</param>
        /// <param name="shutdownCutoffTime">Target cutoff time for the enviroment with shutdown state.</param>
        /// <param name="softDeleteCutoffTime">Target cutoff time for the soft deleted enviroment.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns a list of failed environments.</returns>
        Task<IEnumerable<CloudEnvironment>> GetEnvironmentsReadyForArchiveAsync(
            string idShard,
            int count,
            DateTime shutdownCutoffTime,
            DateTime softDeleteCutoffTime,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Fetch a list of environments that have are ready for terminating. Filter to the target
        /// environments. Specifically where:
        ///     Shard matches target, deleted flag is set, we have not also started
        ///     termination, that the time from last deleted is past our cut off.
        /// </summary>
        /// <param name="idShard">Pool Code.</param>
        /// <param name="cutoffTime">Target cutoff time.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns a list of environments ready for terminate.</returns>
        Task<IEnumerable<CloudEnvironment>> GetEnvironmentsReadyForHardDeleteAsync(
            string idShard,
            DateTime cutoffTime,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Fetch a list of environments that have pending updates.
        /// </summary>
        /// <param name="idShard">Pool Code.</param>
        /// <param name="skuName">Sku to match against.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns a list of environments ready for update.</returns>
        Task<IEnumerable<CloudEnvironment>> GetEnvironmentsToBeUpdatedAsync(
            string idShard,
            string skuName,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the count of how many archive jobs are currently active.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>The number of active jobs.</returns>
        Task<int> GetEnvironmentsArchiveJobActiveCountAsync(
            IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the count of active system updates.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>The number of active jobs.</returns>
        Task<int> GetEnvironmentUpdateJobActiveCountAsync(IDiagnosticsLogger logger);

        /// <summary>
        /// Gets all environments that belong to a specific subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscriptionId.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>The number of active jobs.</returns>
        Task<IEnumerable<CloudEnvironment>> GetAllEnvironmentsInSubscriptionAsync(
            string subscriptionId,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the environment which references the given resource id if it exists.
        /// </summary>
        /// <param name="resourceId">The resource id</param>
        /// <param name="resourceType">The resource type</param>
        /// <param name="logger">The logger</param>
        /// <returns>The environment if one exists</returns>
        Task<CloudEnvironment> GetEnvironmentUsingResource(string resourceId, ResourceType resourceType, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets all environments in the unavailable/failed state that need to be repaired
        /// </summary>
        /// <param name="lastUpdatedDate">The subscriptionId.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>The number of active jobs.</returns>
        Task<IEnumerable<CloudEnvironment>> GetEnvironmentsNeedRepairAsync(DateTime lastUpdatedDate, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the count of unassigned environments for pool.
        /// </summary>
        /// <param name="poolCode">pool definition code.</param>
        /// <param name="logger">logger.</param>
        /// <returns>result.</returns>
        Task<int> GetPoolUnassignedCountAsync(string poolCode, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the list of unassigned environment Ids for pool.
        /// </summary>
        /// <param name="poolCode">pool definition code.</param>
        /// <param name="count">batch size for result.</param>
        /// <param name="logger">logger.</param>
        /// <returns>result.</returns>
        Task<IEnumerable<string>> GetPoolUnassignedAsync(string poolCode, int count, IDiagnosticsLogger logger);
    }
}
