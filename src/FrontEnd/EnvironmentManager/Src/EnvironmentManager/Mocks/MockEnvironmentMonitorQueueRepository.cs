// <copyright file="MockEnvironmentMonitorQueueRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Mock repository for environment monitor.
    /// </summary>
    internal class MockEnvironmentMonitorQueueRepository : IContinuationJobQueueRepository, ICrossRegionContinuationJobQueueRepository
    {
        /// <inheritdoc/>
        public Task AddAsync(string content, TimeSpan? initialVisibilityDelay, IDiagnosticsLogger logger)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task AddAsync(string content, AzureLocation controlPlaneRegion, TimeSpan? initialVisibilityDelay, IDiagnosticsLogger logger)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteAsync(CloudQueueMessage message, IDiagnosticsLogger logger)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<int?> GetApproximateMessageCount(IDiagnosticsLogger logger)
        {
            return Task.FromResult<int?>(0);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<CloudQueueMessage>> GetAsync(int popCount, IDiagnosticsLogger logger, TimeSpan? timeout = null)
        {
            return Task.FromResult(Enumerable.Empty<CloudQueueMessage>());
        }

        /// <inheritdoc/>
        public Task<CloudQueueMessage> GetAsync(IDiagnosticsLogger logger, TimeSpan? timeout = null)
        {
            return Task.FromResult<CloudQueueMessage>(default);
        }
    }
}