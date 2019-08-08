// <copyright file="CloudEnvironmentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repositories;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <inheritdoc/>
    public class CloudEnvironmentManager : ICloudEnvironmentManager
    {
        private const int CloudEnvironmentQuota = 5;
        private const int PersistentSessionExpiresInDays = 30;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEnvironmentManager"/> class.
        /// </summary>
        /// <param name="cloudEnvironmentRepository">The cloud environment repository.</param>
        /// <param name="resourceBrokerClient">The resource broker client.</param>
        /// <param name="workspaceRepository">The Live Share workspace repository.</param>
        /// <param name="sessionSettings">The session settings.</param>
        public CloudEnvironmentManager(
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IResourceBrokerClient resourceBrokerClient,
            IWorkspaceRepository workspaceRepository,
            IOptions<SessionSettings> sessionSettings)
        {
            CloudEnvironmentRepository = Requires.NotNull(cloudEnvironmentRepository, nameof(cloudEnvironmentRepository));
            WorkspaceRepository = Requires.NotNull(workspaceRepository, nameof(workspaceRepository));
            SessionSettings = Requires.NotNull(sessionSettings, nameof(sessionSettings)).Value;
            ResourceBrokerClient = Requires.NotNull(resourceBrokerClient, nameof(resourceBrokerClient));
        }

        private ICloudEnvironmentRepository CloudEnvironmentRepository { get; }

        private IWorkspaceRepository WorkspaceRepository { get; }

        private SessionSettings SessionSettings { get; }

        private IResourceBrokerClient ResourceBrokerClient { get; }

        /// <inheritdoc/>
        public async Task<CloudEnvironment> CreateEnvironment(
            CloudEnvironment cloudEnvironment,
            CloudEnvironmentOptions cloudEnvironmentOptions,
            string currentUserId,
            string accessToken,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
            Requires.NotNull(cloudEnvironmentOptions, nameof(cloudEnvironmentOptions));
            UnauthorizedUtil.IsRequired(currentUserId);
            UnauthorizedUtil.IsRequired(accessToken);
            Requires.NotNull(logger, nameof(logger));

            // Setup
            cloudEnvironment.Id = Guid.NewGuid().ToString();
            cloudEnvironment.OwnerId = currentUserId;
            cloudEnvironment.Created = cloudEnvironment.Updated = DateTime.UtcNow;

            var environments = await CloudEnvironmentRepository.GetWhereAsync((env) => env.OwnerId == cloudEnvironment.OwnerId, logger);

            ValidationUtil.IsTrue(
                !environments.Any((env) => string.Equals(env.FriendlyName, cloudEnvironment.FriendlyName, StringComparison.InvariantCultureIgnoreCase)),
                $"An environment with the friendly name already exists: {cloudEnvironment.FriendlyName}");
            ValidationUtil.IsTrue(
                environments.Count() < CloudEnvironmentQuota,
                $"You have reached the limit of {CloudEnvironmentQuota} environments");

            // Static Environment
            if (cloudEnvironment.Type == CloudEnvironmentType.StaticEnvironment)
            {
                if (cloudEnvironment.Connection == null)
                {
                    cloudEnvironment.Connection = new ConnectionInfo();
                }

                if (string.IsNullOrWhiteSpace(cloudEnvironment.Connection.ConnectionSessionId))
                {
                    cloudEnvironment.Connection.ConnectionSessionId = await CreateWorkspace(cloudEnvironment.Id, logger);
                }

                cloudEnvironment.State = CloudEnvironmentState.Provisioning;
                cloudEnvironment = await CloudEnvironmentRepository.CreateAsync(cloudEnvironment, logger);

                return cloudEnvironment;
            }

            try
            {
                // Create the Live Share workspace
                var sessionId = await CreateWorkspace(cloudEnvironment.Id, logger);
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    // TODO why not throw here?
                    return null;
                }

                // Allocate Storage
                // What about cloudEnvironmentOptions.CreateFileShare
                await AllocateStorage(cloudEnvironment, logger);

                // Allocate Compute
                await AllocateCompute(cloudEnvironment, accessToken, sessionId, logger);

                // Bind compute to storage
                await BindComputeToStorage(cloudEnvironment);

                // TODO: is there any additional step to kick-off initialization of the environment, other than the Bind call?

                // Create the cloud environment record in the provisioining state.
                cloudEnvironment.State = CloudEnvironmentState.Provisioning;
                cloudEnvironment = await CloudEnvironmentRepository.CreateAsync(cloudEnvironment, logger);
                logger.LogInfo("TODO: log success");

                return cloudEnvironment;
            }
            catch (Exception ex)
            {
                logger.LogException("TODO: add create failed", ex);

                // Compensating cleanup
                try
                {
                    await DeleteEnvironmentAsync(cloudEnvironment.Id, currentUserId, logger);
                }
                catch (Exception ex2)
                {
                    logger.LogException("TODO: log cleanup failure", ex2);
                }

                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<CloudEnvironment> UpdateEnvironmentCallbackAsync(
            string id,
            CallbackOptions options,
            string currentUserId,
            IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(id, nameof(id));
            Requires.NotNull(options, nameof(options));
            UnauthorizedUtil.IsRequired(currentUserId);
            Requires.NotNull(logger, nameof(logger));

            var cloudEnvironment = await CloudEnvironmentRepository.GetAsync(id, logger);
            if (cloudEnvironment == null)
            {
                return null;
            }

            UnauthorizedUtil.IsTrue(cloudEnvironment.OwnerId == currentUserId);
            ValidationUtil.IsTrue(cloudEnvironment.Connection.ConnectionSessionId == options.Payload.SessionId);

            cloudEnvironment.Connection.ConnectionSessionPath = options.Payload.SessionPath;
            cloudEnvironment.State = CloudEnvironmentState.Available;

            return await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, logger);
        }

        /// <inheritdoc/>
        public async Task<CloudEnvironment> GetEnvironmentAsync(
            string id,
            string currentUserId,
            IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(id, nameof(id));
            Requires.NotNull(logger, nameof(logger));
            UnauthorizedUtil.IsRequired(currentUserId);

            var cloudEnvironment = await CloudEnvironmentRepository.GetAsync(id, logger);
            if (cloudEnvironment == null)
            {
                return null;
            }

            UnauthorizedUtil.IsTrue(cloudEnvironment.OwnerId == currentUserId);

            // Note: We do not do this in the case of GetListByOwnerAsync, because
            // Will require multiple calls to workspace service, causing un-necessary slowness and
            // No API as of now to pass multiple workspaceIds
            var workspace = await WorkspaceRepository.GetStatusAsync(cloudEnvironment.Connection.ConnectionSessionId);
            if (workspace == null)
            {
                // In this case the workspace is deleted. There is no way of getting to an environment without it.
                cloudEnvironment.State = CloudEnvironmentState.Unavailable;
            }
            else
            {
                if (workspace.IsHostConnected.HasValue)
                {
                    cloudEnvironment.State = workspace.IsHostConnected.Value ? CloudEnvironmentState.Available : CloudEnvironmentState.Awaiting;
                }

                // else we don't change the model state.
            }

            return cloudEnvironment;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> GetEnvironmentsByOwnerAsync(
            string currentUserId,
            IDiagnosticsLogger logger)
        {
            UnauthorizedUtil.IsRequired(currentUserId);
            Requires.NotNull(logger, nameof(logger));
            return await CloudEnvironmentRepository.GetWhereAsync((cloudEnvironment) => cloudEnvironment.OwnerId == currentUserId, logger);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteEnvironmentAsync(
            string id,
            string currentUserId,
            IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(id, nameof(id));
            Requires.NotNullOrEmpty(currentUserId, nameof(currentUserId));
            Requires.NotNull(logger, nameof(logger));

            var cloudEnvironment = await CloudEnvironmentRepository.GetAsync(id, logger);
            if (cloudEnvironment == null)
            {
                return false;
            }

            UnauthorizedUtil.IsTrue(cloudEnvironment.OwnerId == currentUserId);

            if (cloudEnvironment.Type == CloudEnvironmentType.CloudEnvironment)
            {
                var storageIdToken = cloudEnvironment.Storage?.ResourceIdToken;
                if (storageIdToken != null)
                {
                    await ResourceBrokerClient.DeallocateAsync(storageIdToken);
                }

                var computeIdToken = cloudEnvironment.Compute?.ResourceIdToken;
                if (computeIdToken != null)
                {
                    await ResourceBrokerClient.DeallocateAsync(computeIdToken);
                }
            }

            await WorkspaceRepository.DeleteAsync(cloudEnvironment.Connection.ConnectionSessionId);
            await CloudEnvironmentRepository.DeleteAsync(id, logger);

            return true;
        }

        private async Task<string> CreateWorkspace(
            string id,
            IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(id, nameof(id));

            var workspaceRequest = new WorkspaceRequest()
            {
                Name = id,
                ConnectionMode = ConnectionMode.Auto,
                AreAnonymousGuestsAllowed = false,
                ExpiresAt = DateTime.UtcNow.AddDays(PersistentSessionExpiresInDays),
            };

            var workspaceResponse = await WorkspaceRepository.CreateAsync(workspaceRequest);
            if (string.IsNullOrWhiteSpace(workspaceResponse.Id))
            {
                logger
                    .AddEnvironmentId(id)
                    .LogError("workspace_creation_failed");
                return null;
            }

            return workspaceResponse.Id;
        }

        private async Task AllocateCompute(
            CloudEnvironment cloudEnvironment,
            string accessToken,
            string sessionId,
            IDiagnosticsLogger logger)
        {
            var environmentVariables = EnvironmentVariableGenerator.Generate(
                cloudEnvironment,
                SessionSettings,
                accessToken,
                sessionId);

            var compute = await ResourceBrokerClient.AllocateAsync(
                new AllocateInput
                {
                    Type = ResourceType.ComputeVM,
                    SkuName = cloudEnvironment.SkuName,
                    Location = cloudEnvironment.Location,
                    EnvironmentVariables = environmentVariables,
                },
                logger);

            cloudEnvironment.Compute = new ResourceAllocation
            {
                ResourceIdToken = compute.ResourceIdToken,
                SkuName = compute.SkuName,
                Location = compute.Location,
                Created = compute.Created,
            };

            // Setup the connection
            cloudEnvironment.Connection = new ConnectionInfo
            {
                ConnectionSessionId = sessionId,

                // TODO: is this the underlying compute VM id?
                ConnectionComputeId = cloudEnvironment.Compute.ResourceIdToken,
            };
        }

        private async Task AllocateStorage(
            CloudEnvironment cloudEnvironment,
            IDiagnosticsLogger logger)
        {
            var storage = await ResourceBrokerClient.AllocateAsync(
                new AllocateInput
                {
                    // TODO: real input values go here!
                    Type = ResourceType.Storage,
                    SkuName = cloudEnvironment.SkuName,
                    Location = cloudEnvironment.Location,
                },
                logger);

            cloudEnvironment.Storage = new ResourceAllocation
            {
                ResourceIdToken = storage.ResourceIdToken,
                SkuName = storage.SkuName,
                Location = storage.Location,
                Created = storage.Created,
            };
        }

        private async Task BindComputeToStorage(
            CloudEnvironment cloudEnvironment)
        {
            _ = await ResourceBrokerClient.BindComputeToStorage(new BindInput
            {
                ComputeResourceIdToken = cloudEnvironment.Compute.ResourceIdToken,
                StorageResourceIdToken = cloudEnvironment.Storage.ResourceIdToken,
            });
        }
    }
}
