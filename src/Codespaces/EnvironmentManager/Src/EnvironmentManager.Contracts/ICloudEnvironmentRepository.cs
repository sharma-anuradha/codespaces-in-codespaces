// <copyright file="ICloudEnvironmentRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// A repository that wraps an <see cref="IGlobalCloudEnvironmentRepository"/> and an <see cref="IRegionalCloudEnvironmentRepository"/>.
    /// </summary>
    public interface ICloudEnvironmentRepository : IEntityRepository<CloudEnvironment>
    {
        /// <summary>
        /// Gets the global cloud environment repository.
        /// </summary>
        /// <value>The global cloud environment repository.</value>
        IGlobalCloudEnvironmentRepository GlobalRepository { get; }

        /// <summary>
        /// Gets the regional cloud environment repository.
        /// </summary>
        /// <value>The regional cloud environment repository.</value>
        IRegionalCloudEnvironmentRepository RegionalRepository { get; }

        /// <summary>
        /// Add a new cloud environment record to the repository.
        /// </summary>
        /// <param name="environment">The cloud environment.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The created cloud environment.</returns>
        Task<CloudEnvironment> CreateAsync(CloudEnvironment environment, IDiagnosticsLogger logger);

        /// <summary>
        /// Delete a cloud environment record in the repository.
        /// </summary>
        /// <param name="id">The id of the cloud environment to delete.</param>
        /// <param name="logger">The logger.</param>
        /// <returns><c>true</c> if the cloud environment record was deleted; otherwise, <c>false</c>.</returns>
        Task<bool> DeleteAsync(DocumentDbKey id, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the list of environments for the specified plan.
        /// </summary>
        /// <param name="planId">The plan resource id.</param>
        /// <param name="location">The plan location, if known.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The list of environments.</returns>
        Task<IEnumerable<CloudEnvironment>> ListAsync(string planId, AzureLocation? location, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the list of environments for the specified plan.
        /// </summary>
        /// <param name="planId">The plan resource id.</param>
        /// <param name="location">The plan location, if known.</param>
        /// <param name="friendlyNameInLowerCase">The lowercased friendly name of the environment.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The list of environments.</returns>
        Task<IEnumerable<CloudEnvironment>> ListAsync(string planId, AzureLocation? location, string friendlyNameInLowerCase, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the list of environments for the specified user.
        /// </summary>
        /// <param name="userIdSet">The user id.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The list of environments.</returns>
        Task<IEnumerable<CloudEnvironment>> ListAsync(UserIdSet userIdSet, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the list of environments for the specified user.
        /// </summary>
        /// <param name="userIdSet">The user id.</param>
        /// <param name="friendlyNameInLowerCase">The lowercased friendly name of the environment.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The list of environments.</returns>
        Task<IEnumerable<CloudEnvironment>> ListAsync(UserIdSet userIdSet, string friendlyNameInLowerCase, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the list of environments for the specified plan and user.
        /// </summary>
        /// <param name="planId">The plan resource id.</param>
        /// <param name="location">The plan location, if known.</param>
        /// <param name="userIdSet">The user id.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The list of environments.</returns>
        Task<IEnumerable<CloudEnvironment>> ListAsync(string planId, AzureLocation? location, UserIdSet userIdSet, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the list of environments for the specified plan and user.
        /// </summary>
        /// <param name="planId">The plan resource id.</param>
        /// <param name="location">The plan location, if known.</param>
        /// <param name="userIdSet">The user id.</param>
        /// <param name="friendlyNameInLowerCase">The lowercased friendly name of the environment.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The list of environments.</returns>
        Task<IEnumerable<CloudEnvironment>> ListAsync(string planId, AzureLocation? location, UserIdSet userIdSet, string friendlyNameInLowerCase, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets all environments that belong to a specific subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscriptionId.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>The number of active jobs.</returns>
        Task<IEnumerable<CloudEnvironment>> GetAllEnvironmentsInSubscriptionAsync(string subscriptionId, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the count of unique plans that have Environments in the cloud environments table.
        /// </summary>
        /// <param name="logger">the logger.</param>
        /// <returns>The number of Unique plans in the cloud environments table.</returns>
        Task<int> GetCloudEnvironmentPlanCountAsync(IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the count of unique subscriptions that have Environments in the cloud environments table.
        /// </summary>
        /// <param name="logger">the logger.</param>
        /// <returns>The number of Unique subscriptions in the cloud environments table.</returns>
        Task<int> GetCloudEnvironmentSubscriptionCountAsync(IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the count of how many archive jobs are currently active.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>The number of active jobs.</returns>
        Task<int> GetEnvironmentsArchiveJobActiveCountAsync(IDiagnosticsLogger logger);

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
        /// <returns>Returns a list of environments ready for archive.</returns>
        Task<IEnumerable<CloudEnvironment>> GetEnvironmentsReadyForArchiveAsync(
            string idShard,
            int count,
            DateTime shutdownCutoffTime,
            DateTime softDeleteCutoffTime,
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
        /// Iterate over each cloud environment in the specified control-plane location that has compute or storage resources.
        /// </summary>
        /// <param name="controlPlaneLocation">The control-plane location.</param>
        /// <param name="shardId">The shard id.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="itemCallback">The item callback.</param>
        /// <param name="pageResultsCallback">THe page results callback.</param>
        /// <returns>An awaitable task.</returns>
        Task ForEachEnvironmentWithComputeOrStorageAsync(AzureLocation controlPlaneLocation, string shardId, IDiagnosticsLogger logger, Func<CloudEnvironment, IDiagnosticsLogger, Task> itemCallback, Func<IEnumerable<CloudEnvironment>, IDiagnosticsLogger, Task> pageResultsCallback = null);
    }
}
