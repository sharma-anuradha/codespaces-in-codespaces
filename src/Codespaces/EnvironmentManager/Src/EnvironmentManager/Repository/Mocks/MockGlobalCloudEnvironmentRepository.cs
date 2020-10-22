// <copyright file="MockGlobalCloudEnvironmentRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repository.Mocks
{
    /// <summary>
    /// A mock cloud environment repository.
    /// </summary>
    public class MockGlobalCloudEnvironmentRepository : MockRepository<CloudEnvironment>, IGlobalCloudEnvironmentRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockGlobalCloudEnvironmentRepository"/> class.
        /// </summary>
        /// <param name="location">The mock control-plane location.</param>
        public MockGlobalCloudEnvironmentRepository(AzureLocation location = AzureLocation.WestUs2)
        {
            ControlPlaneLocation = location;
        }

        /// <inheritdoc/>
        public AzureLocation ControlPlaneLocation { get; }

        /// <inheritdoc/>
        public Task<int> GetCloudEnvironmentPlanCountAsync(IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<int> GetCloudEnvironmentSubscriptionCountAsync(IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IEnumerable<CloudEnvironment>> GetEnvironmentsReadyForArchiveAsync(string idShard, int count, DateTime shutdownCutoffTime, DateTime softDeleteCutoffTime, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IEnumerable<CloudEnvironment>> GetFailedOperationAsync(string idShard, int count, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<int> GetEnvironmentsArchiveJobActiveCountAsync(IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IEnumerable<CloudEnvironment>> GetAllEnvironmentsInSubscriptionAsync(string subscriptionId, IDiagnosticsLogger logger)
        {
            return GetWhereAsync(t => t.PlanId.Contains(subscriptionId), logger.NewChildLogger());
        }

        /// <inheritdoc/>
        public Task<IEnumerable<CloudEnvironment>> GetEnvironmentsReadyForHardDeleteAsync(string idShard, DateTime cutoffTime, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IEnumerable<CloudEnvironment>> GetEnvironmentsToBeUpdatedAsync(
            string idShard,
            string skuName,
            IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<int> GetEnvironmentUpdateJobActiveCountAsync(IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> GetEnvironmentUsingResource(string resourceId, ResourceType resourceType, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IEnumerable<CloudEnvironment>> GetEnvironmentsNeedRepairAsync(DateTime lastUpdatedDate, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<int> GetPoolUnassignedCountAsync(string poolCode, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IEnumerable<string>> GetPoolUnassignedAsync(string poolCode, int count, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }
    }
}
