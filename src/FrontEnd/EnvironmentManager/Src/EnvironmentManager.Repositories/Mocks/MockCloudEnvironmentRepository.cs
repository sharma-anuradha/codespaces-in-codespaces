// <copyright file="MockCloudEnvironmentRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repositories.Mocks
{
    /// <summary>
    /// An in-memory cloud environment repository.
    /// </summary>
    public class MockCloudEnvironmentRepository : MockRepository<CloudEnvironment>, ICloudEnvironmentRepository
    {
        /// <inheritdoc/>
        public Task<int> GetCloudEnvironmentCountAsync(string location, string state, string skuName, IDiagnosticsLogger logger)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<int> GetCloudEnvironmentPlanCountAsync(IDiagnosticsLogger logger)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<int> GetCloudEnvironmentSubscriptionCountAsync(IDiagnosticsLogger logger)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IEnumerable<CloudEnvironment>> GetEnvironmentsReadyForArchiveAsync(string idShard, int count, DateTime cutoffTime, AzureLocation locaiton, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IEnumerable<CloudEnvironment>> GetFailedOperationAsync(string idShard, int count, AzureLocation locaiton, IDiagnosticsLogger logger)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<int> GetEnvironmentsArchiveJobActiveCountAsync(AzureLocation controlPlaneLocation, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }
    }
}
