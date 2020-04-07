// <copyright file="MockEnvironmentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Mocks
{
    /// <summary>
    /// The mock cloud environment manager.
    /// </summary>
    public class MockEnvironmentManager : IEnvironmentManager
    {
        private CloudEnvironment cloudEnvironment;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockEnvironmentManager"/> class.
        /// </summary>
        public MockEnvironmentManager()
        {
            cloudEnvironment = new CloudEnvironment() { LastUsed = DateTime.UtcNow.AddDays(-1), };
        }

        /// <inheritdoc/>
        public Task<(ResourceAllocation Compute, ResourceAllocation Storage)> AllocateComputeAndStorageAsync(CloudEnvironment cloudEnvironment, bool queueResourceCreation, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CloudEnvironmentServiceResult> CreateAsync(CloudEnvironment environmentRegistration, CloudEnvironmentOptions options, StartCloudEnvironmentParameters startCloudEnvironmentParameters, VsoPlanInfo plan, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<ConnectionInfo> CreateWorkspace(EnvironmentType type, string cloudEnvironmentId, Guid computeIdToken, Uri connectionServiceUri, string sessionPath, string userAuthToken, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(CloudEnvironment cloudEnvironment, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CloudEnvironmentServiceResult> ForceSuspendAsync(CloudEnvironment cloudEnvironment, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> GetAndStateRefreshAsync(string environmentId, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> GetAsync(string environmentId, IDiagnosticsLogger logger)
        {
            return Task.FromResult(cloudEnvironment);
        }

        /// <inheritdoc/>
        public Task<CloudEnvironmentAvailableSettingsUpdates> GetAvailableSettingsUpdatesAsync(CloudEnvironment cloudEnvironment, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IEnumerable<CloudEnvironment>> ListAsync(IDiagnosticsLogger logger, string planId = null, string name = null, UserIdSet userIdSet = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CloudEnvironmentServiceResult> ResumeAsync(CloudEnvironment cloudEnvironment, StartCloudEnvironmentParameters startCloudEnvironmentParameters, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> ResumeCallbackAsync(CloudEnvironment cloudEnvironment, Guid storageResourceId, Guid? archiveStorageResourceId, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<bool> StartComputeAsync(CloudEnvironment cloudEnvironment, Guid computeResourceId, Guid storageResourceId, Guid? archiveStorageResourceId, CloudEnvironmentOptions cloudEnvironmentOptions, StartCloudEnvironmentParameters startCloudEnvironmentParameters, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CloudEnvironmentServiceResult> SuspendAsync(CloudEnvironment cloudEnvironment, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CloudEnvironmentServiceResult> SuspendCallbackAsync(CloudEnvironment cloudEnvironment, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> UpdateAsync(CloudEnvironment cloudEnvironment, CloudEnvironmentState newState, string trigger, string reason, IDiagnosticsLogger logger)
        {
            this.cloudEnvironment = cloudEnvironment;
            return Task.FromResult(this.cloudEnvironment);
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> UpdateCallbackAsync(CloudEnvironment cloudEnvironment, EnvironmentRegistrationCallbackOptions options, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CloudEnvironmentSettingsUpdateResult> UpdateSettingsAsync(CloudEnvironment cloudEnvironment, CloudEnvironmentUpdate update, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }
    }
}
