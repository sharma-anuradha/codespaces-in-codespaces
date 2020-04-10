// <copyright file="IPlanRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans
{
    /// <summary>
    /// Represents a VSO plan repository.
    /// </summary>
    public interface IPlanRepository : IDocumentDbCollection<VsoPlan>
    {
        /// <summary>
        /// Gets the count of all plans in the plans repository.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <returns>the total count of plans.</returns>
        Task<int> GetCountAsync(IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the count of unique subscriptions represented in the plans table.
        /// </summary>
        /// <param name="logger">the logger.</param>
        /// <returns>The number of unique subscriptions that have plans.</returns>
        Task<int> GetPlanSubscriptionCountAsync(IDiagnosticsLogger logger);
    }
}
