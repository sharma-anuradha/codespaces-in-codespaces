// <copyright file="MockEnvironmentContinuation.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Mocks
{
    /// <summary>
    /// Mock class.
    /// </summary>
    public class MockEnvironmentContinuation : IEnvironmentContinuationOperations
    {
        /// <inheritdoc/>
        public Task<ContinuationResult> ArchiveAsync(Guid environmentId, DateTime lastStateUpdated, string reason, IDiagnosticsLogger logger)
        {
            return Task.FromResult(new ContinuationResult() { });
        }

        /// <inheritdoc/>
        public Task<ContinuationResult> CreateAsync(Guid environmentId, DateTime lastStateUpdated, CloudEnvironmentOptions cloudEnvironmentOptions, StartCloudEnvironmentParameters startCloudEnvironmentParameters, string reason, IDiagnosticsLogger logger)
        {
            return Task.FromResult(new ContinuationResult() { });
        }

        /// <inheritdoc/>
        public Task<ContinuationResult> ResumeAsync(Guid environmentId, DateTime lastStateUpdated, StartCloudEnvironmentParameters startCloudEnvironmentParameters, string reason, IDiagnosticsLogger logger)
        {
            return Task.FromResult(new ContinuationResult() { });
        }

        /// <inheritdoc/>
        public Task<ContinuationResult> ShutdownAsync(Guid environmentId, bool forceSuspend, string reason, IDiagnosticsLogger logger)
        {
            return Task.FromResult(new ContinuationResult() { });
        }
    }
}
