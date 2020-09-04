// <copyright file="MockEnvironmentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;

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
        public Task<CloudEnvironment> CreateAsync(
            EnvironmentCreateDetails details,
            StartCloudEnvironmentParameters startEnvironmentParams,
            MetricsInfo metricsInfo,
            IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> GetAsync(Guid environmentId, IDiagnosticsLogger logger)
        {
            return Task.FromResult(cloudEnvironment);
        }

        /// <inheritdoc/>
        public Task<CloudEnvironmentAvailableSettingsUpdates> GetAvailableSettingsUpdatesAsync(CloudEnvironment cloudEnvironment, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IEnumerable<CloudEnvironment>> ListAsync(string planId, string name, UserIdSet userIdSet, EnvironmentListType environmentListType, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<bool> StartComputeAsync(
            CloudEnvironment cloudEnvironment,
            Guid computeResourceId,
            Guid? osDiskResourceId,
            Guid? storageResourceId,
            Guid? archiveStorageResourceId,
            CloudEnvironmentParameters cloudEnvironmentParameters,
            StartEnvironmentAction startEnvironmentAction,
            IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CloudEnvironmentServiceResult> SuspendCallbackAsync(CloudEnvironment cloudEnvironment, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> UpdateAsync(CloudEnvironment cloudEnvironment, CloudEnvironmentState newState, string trigger, string reason, bool? isUserError, IDiagnosticsLogger logger)
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
        public Task<CloudEnvironmentUpdateResult> UpdateSettingsAsync(CloudEnvironment cloudEnvironment, CloudEnvironmentUpdate update, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CloudEnvironmentUpdateResult> UpdateFoldersListAsync(CloudEnvironment cloudEnvironment, CloudEnvironmentFolderBody update, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> UpdateStatusAsync(Guid cloudEnvironmentId, CloudEnvironmentState newState, string trigger, string reason, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IEnumerable<CloudEnvironment>> ListBySubscriptionAsync(Subscription subscription, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<bool> HardDeleteAsync(Guid environmentId, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<bool> SoftDeleteAsync(Guid cloudEnvironmentId, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> DeleteRestoreAsync(Guid cloudEnvironmentId, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> ResumeAsync(Guid environmentId, StartCloudEnvironmentParameters startCloudEnvironmentParameters, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> ResumeCallbackAsync(Guid environmentId, Guid storageResourceId, Guid? archiveStorageResourceId, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> SuspendAsync(Guid environmentId, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> ForceSuspendAsync(Guid environmentId, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> ExportAsync(Guid cloudEnvironment, ExportCloudEnvironmentParameters exportCloudEnvironmentParameters, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> ExportCallbackAsync(Guid cloudEnvironment, Guid storageResourceId, Guid? archiveStorageResourceId, string exportedEnvironmentUrl, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }
    }
}
