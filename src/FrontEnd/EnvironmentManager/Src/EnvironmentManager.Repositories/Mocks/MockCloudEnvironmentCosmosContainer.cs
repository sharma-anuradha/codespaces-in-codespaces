// <copyright file="MockCloudEnvironmentCosmosContainer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaas.Azure.Cosmos;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repositories.Mocks
{
    /// <summary>
    /// A mock cloud environment repository.
    /// </summary>
    public class MockCloudEnvironmentCosmosContainer : MockCosmosContainer<CloudEnvironment>, ICloudEnvironmentCosmosContainer
    {
        /// <inheritdoc/>
        public async Task<QueryResults<CloudEnvironmentCountByDimensions>> GetCountByDimensionsAsync(AzureLocation location, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;
            var results = new List<CloudEnvironmentCountByDimensions>().AsReadOnly();
            return new QueryResults<CloudEnvironmentCountByDimensions>(results, 0.0);
        }
    }
}
