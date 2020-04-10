// <copyright file="ICloudEnvironmentCosmosContainer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaas.Azure.Cosmos;
using Microsoft.VsSaaS.Azure.Cosmos;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// A Cosmos DB container of <see cref="CloudEnvironment"/>.
    /// </summary>
    public interface ICloudEnvironmentCosmosContainer : ICosmosContainer<CloudEnvironment>
    {
        /// <summary>
        /// Gets the count of environments grouped by sku, location, state, and partner;
        /// where environment location is <paramref name="location"/>.
        /// </summary>
        /// <param name="location">the location.</param>
        /// <param name="logger">the logger.</param>
        /// <returns>the number of cloud environments in the input state.</returns>
        Task<QueryResults<CloudEnvironmentCountByDimensions>> GetCountByDimensionsAsync(
            AzureLocation location,
            IDiagnosticsLogger logger);
    }
}
