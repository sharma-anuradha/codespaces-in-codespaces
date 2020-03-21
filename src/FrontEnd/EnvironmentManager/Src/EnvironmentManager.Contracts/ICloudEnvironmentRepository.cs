// <copyright file="ICloudEnvironmentRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// A repository of <see cref="CloudEnvironment"/>.
    /// </summary>
    public interface ICloudEnvironmentRepository : IDocumentDbCollection<CloudEnvironment>
    {
        /// <summary>
        /// Counts the number of environments in a given state.
        /// </summary>
        /// <param name="location">the location being looked at.</param>
        /// <param name="state">The state that's desired to be counted.</param>
        /// <param name="skuName">The sku count being sought after.</param>
        /// <param name="logger">the logger.</param>
        /// <returns>the number of cloud environments in the input state.</returns>
        Task<int> GetCloudEnvironmentCountAsync(
            string location,
            string state,
            string skuName,
            IDiagnosticsLogger logger);

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
        /// <param name="cutoffTime">Target cutoff time.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns a list of failed environments.</returns>
        Task<IEnumerable<CloudEnvironment>> GetEnvironmentsReadyForArchiveAsync(
            string idShard,
            int count,
            DateTime cutoffTime,
            IDiagnosticsLogger logger);
    }
}
