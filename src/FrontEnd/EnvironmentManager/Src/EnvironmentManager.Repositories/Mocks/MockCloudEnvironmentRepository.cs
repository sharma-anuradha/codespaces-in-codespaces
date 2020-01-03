// <copyright file="MockCloudEnvironmentRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repositories.Mocks
{
    /// <summary>
    /// An in-memory cloud environment repository.
    /// </summary>
    public class MockCloudEnvironmentRepository : MockRepository<CloudEnvironment>, ICloudEnvironmentRepository
    {
        public Task<int> GetCloudEnvironmentCountAsync(string location, string state, string skuName, IDiagnosticsLogger logger)
        {
            throw new System.NotImplementedException();
        }

        public Task<int> GetCloudEnvironmentPlanCountAsync(IDiagnosticsLogger logger)
        {
            throw new System.NotImplementedException();
        }

        public Task<int> GetCloudEnvironmentSubscriptionCountAsync(IDiagnosticsLogger logger)
        {
            throw new System.NotImplementedException();
        }
    }
}
