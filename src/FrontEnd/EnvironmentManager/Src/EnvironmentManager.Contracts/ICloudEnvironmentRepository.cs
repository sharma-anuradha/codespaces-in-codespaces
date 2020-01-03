// <copyright file="ICloudEnvironmentRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

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
        Task<int> GetCloudEnvironmentCountAsync(string location, string state, string skuName, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the count of unique subscriptions that have Environments in the cloud environments table.
        /// </summary>
        /// <param name="logger">the logger</param>
        /// <returns>The number of Unique subscriptions in the cloud environments table.</returns>
        Task<int> GetCloudEnvironmentSubscriptionCountAsync(IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the count of unique plans that have Environments in the cloud environments table.
        /// </summary>
        /// <param name="logger">the logger.</param>
        /// <returns>The number of Unique plans in the cloud environments table.</returns>
        Task<int> GetCloudEnvironmentPlanCountAsync(IDiagnosticsLogger logger);
    }
}
