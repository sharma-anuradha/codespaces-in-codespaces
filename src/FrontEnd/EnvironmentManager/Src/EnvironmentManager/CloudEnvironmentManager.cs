// <copyright file="CloudEnvironmentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
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
        /// <param name="resourceBrokerHttpClient">The resource broker client.</param>
        /// <param name="workspaceRepository">The Live Share workspace repository.</param>
        /// <param name="sessionSettings">The session settings.</param>
        public CloudEnvironmentManager(
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IResourceBrokerResourcesHttpContract resourceBrokerHttpClient,
            IWorkspaceRepository workspaceRepository,
            IOptions<SessionSettings> sessionSettings)
        {
            CloudEnvironmentRepository = Requires.NotNull(cloudEnvironmentRepository, nameof(cloudEnvironmentRepository));
            WorkspaceRepository = Requires.NotNull(workspaceRepository, nameof(workspaceRepository));
            SessionSettings = Requires.NotNull(sessionSettings, nameof(sessionSettings)).Value;
            ResourceBrokerClient = Requires.NotNull(resourceBrokerHttpClient, nameof(resourceBrokerHttpClient));
        }

        private ICloudEnvironmentRepository CloudEnvironmentRepository { get; }

        private IWorkspaceRepository WorkspaceRepository { get; }

        private SessionSettings SessionSettings { get; }

        private IResourceBrokerResourcesHttpContract ResourceBrokerClient { get; }

        /// <inheritdoc/>
        public async Task<CloudEnvironment> CreateEnvironmentAsync(
            CloudEnvironment cloudEnvironment,
            CloudEnvironmentOptions cloudEnvironmentOptions,
            string currentUserId,
            string accessToken,
            IDiagnosticsLogger logger)
        {
            var duration = logger.StartDuration();

            try
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
                ValidationUtil.IsRequired(cloudEnvironment.SkuName, nameof(cloudEnvironment.SkuName));
                ValidationUtil.IsTrue(
                    cloudEnvironment.Location != default,
                    "Location is required");

                // Static Environment
                if (cloudEnvironment.Type == CloudEnvironmentType.StaticEnvironment)
                {
                    if (cloudEnvironment.Connection is null)
                    {
                        cloudEnvironment.Connection = new ConnectionInfo();
                    }

                    if (string.IsNullOrWhiteSpace(cloudEnvironment.Connection.ConnectionSessionId))
                    {
                        cloudEnvironment.Connection.ConnectionSessionId = await CreateWorkspace(cloudEnvironment.Id, logger);
                    }

                    cloudEnvironment.State = CloudEnvironmentState.Provisioning;
                    cloudEnvironment = await CloudEnvironmentRepository.CreateAsync(cloudEnvironment, logger);

                    logger.AddDuration(duration)
                        .AddCloudEnvironment(cloudEnvironment)
                        .LogInfo(GetType().FormatLogMessage(nameof(CreateEnvironmentAsync)));

                    return cloudEnvironment;
                }

                // Create the Live Share workspace
                var sessionId = await CreateWorkspace(cloudEnvironment.Id, logger);
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    logger.AddDuration(duration)
                        .AddCloudEnvironment(cloudEnvironment)
                        .AddSessionId(sessionId)
                        .LogError(GetType().FormatLogErrorMessage(nameof(CreateEnvironmentAsync)));
                    throw new InvalidOperationException("Cloud not create the cloud environment workspace session.");
                }

                // Allocate Storage
                // What about cloudEnvironmentOptions.CreateFileShare
                await AllocateStorage(cloudEnvironment, logger);

                // Allocate Compute
                var computeResult = await AllocateCompute(cloudEnvironment, accessToken, sessionId, logger);

                // Bind compute to storage
                await StartCompute(cloudEnvironment, computeResult.EnvironmentVariables, logger);

                // TODO: is there any additional step to kick-off initialization of the environment, other than the Bind call?

                // Create the cloud environment record in the provisioining state.
                cloudEnvironment.State = CloudEnvironmentState.Provisioning;
                cloudEnvironment = await CloudEnvironmentRepository.CreateAsync(cloudEnvironment, logger);

                logger.AddDuration(duration)
                    .AddCloudEnvironment(cloudEnvironment)
                    .LogInfo(GetType().FormatLogMessage(nameof(CreateEnvironmentAsync)));

                return cloudEnvironment;
            }
            catch (Exception ex)
            {
                logger
                    .AddDuration(duration)
                    .AddCloudEnvironment(cloudEnvironment)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(CreateEnvironmentAsync)), ex.Message);

                try
                {
                    /*
                     * TODO: This won't actually cleanup properly because it relies on the workspace,
                     * compute, and storage to have been written to the database!
                     */
                    // Compensating cleanup
                    await DeleteEnvironmentAsync(cloudEnvironment.Id, currentUserId, logger);
                }
                catch (Exception ex2)
                {
                    throw new AggregateException(
                        GetType().FormatLogErrorMessage(nameof(CreateEnvironmentAsync)),
                        ex,
                        ex2);
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
            var duration = logger.StartDuration();

            var cloudEnvironment = default(CloudEnvironment);

            try
            {
                Requires.NotNullOrEmpty(id, nameof(id));
                Requires.NotNull(options, nameof(options));
                UnauthorizedUtil.IsRequired(currentUserId);
                Requires.NotNull(logger, nameof(logger));

                cloudEnvironment = await CloudEnvironmentRepository.GetAsync(id, logger);
                if (cloudEnvironment is null)
                {
                    // TODO why not throw here instead of null?
                    // the operation has actually failed?
                    return null;
                }

                UnauthorizedUtil.IsTrue(cloudEnvironment.OwnerId == currentUserId);
                ValidationUtil.IsTrue(cloudEnvironment.Connection.ConnectionSessionId == options.Payload.SessionId);

                cloudEnvironment.Connection.ConnectionSessionPath = options.Payload.SessionPath;
                cloudEnvironment.State = CloudEnvironmentState.Available;
                var result = await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, logger);

                logger.AddDuration(duration)
                    .AddCloudEnvironment(cloudEnvironment)
                    .LogInfo(GetType().FormatLogMessage(nameof(UpdateEnvironmentCallbackAsync)));

                return result;
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .AddCloudEnvironment(cloudEnvironment)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(UpdateEnvironmentCallbackAsync)), ex.Message);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<CloudEnvironment> GetEnvironmentAsync(
            string id,
            string currentUserId,
            IDiagnosticsLogger logger)
        {
            var duration = logger.StartDuration();
            var cloudEnvironment = default(CloudEnvironment);

            try
            {
                Requires.NotNullOrEmpty(id, nameof(id));
                Requires.NotNull(logger, nameof(logger));
                UnauthorizedUtil.IsRequired(currentUserId);

                cloudEnvironment = await CloudEnvironmentRepository.GetAsync(id, logger);
                if (cloudEnvironment is null)
                {
                    return null;
                }

                UnauthorizedUtil.IsTrue(cloudEnvironment.OwnerId == currentUserId);

                // Note: We do not do this in the case of GetListByOwnerAsync, because
                // Will require multiple calls to workspace service, causing un-necessary slowness and
                // No API as of now to pass multiple workspaceIds
                var workspace = await WorkspaceRepository.GetStatusAsync(cloudEnvironment.Connection.ConnectionSessionId);
                if (workspace is null)
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
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .AddCloudEnvironment(cloudEnvironment)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(GetEnvironmentAsync)), ex.Message);
                throw;
            }
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

            /*
            // TODO: this delete logic isn't complete!
            // It should handle Workspace, Storage, and Compute, and CosmosDB independently.
            */

            var cloudEnvironment = await CloudEnvironmentRepository.GetAsync(id, logger);
            if (cloudEnvironment is null)
            {
                return false;
            }

            UnauthorizedUtil.IsTrue(cloudEnvironment.OwnerId == currentUserId);

            if (cloudEnvironment.Type == CloudEnvironmentType.CloudEnvironment)
            {
                var storageIdToken = cloudEnvironment.Storage?.ResourceIdToken;
                if (storageIdToken != null)
                {
                    await ResourceBrokerClient.DeleteResourceAsync(storageIdToken, logger);
                }

                var computeIdToken = cloudEnvironment.Compute?.ResourceIdToken;
                if (computeIdToken != null)
                {
                    await ResourceBrokerClient.DeleteResourceAsync(computeIdToken, logger);
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
            var duration = logger.StartDuration();

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
                    .LogError(GetType().FormatLogErrorMessage(nameof(CreateWorkspace)));
                return null;
            }

            return workspaceResponse.Id;
        }

        private async Task<AllocateComputeResult> AllocateCompute(
            CloudEnvironment cloudEnvironment,
            string accessToken,
            string sessionId,
            IDiagnosticsLogger logger)
        {
            var compute = await ResourceBrokerClient.CreateResourceAsync(
                new CreateResourceRequestBody
                {
                    Type = ResourceType.ComputeVM,
                    SkuName = cloudEnvironment.SkuName,
                    Location = cloudEnvironment.Location,
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

                // TODO: is this a DUP of the underlying compute VM id?
                ConnectionComputeId = cloudEnvironment.Compute.ResourceIdToken,
            };

            // Construct the environment variables
            var environmentVariables = EnvironmentVariableGenerator.Generate(
                cloudEnvironment,
                SessionSettings,
                accessToken,
                sessionId);

            return new AllocateComputeResult
            {
                EnvironmentVariables = environmentVariables,
            };
        }

        private async Task AllocateStorage(
            CloudEnvironment cloudEnvironment,
            IDiagnosticsLogger logger)
        {
            var storage = await ResourceBrokerClient.CreateResourceAsync(
                new CreateResourceRequestBody
                {
                    // TODO: real input values go here!
                    Type = ResourceType.StorageFileShare,
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

        private async Task StartCompute(
            CloudEnvironment cloudEnvironment,
            Dictionary<string, string> environmentVariables,
            IDiagnosticsLogger logger)
        {
            ValidationUtil.IsRequired(cloudEnvironment?.Compute?.ResourceIdToken, nameof(cloudEnvironment.Compute.ResourceIdToken));

            _ = await ResourceBrokerClient.StartComputeAsync(
                cloudEnvironment.Compute.ResourceIdToken,
                new StartComputeRequestBody
                {
                    StorageResourceIdToken = cloudEnvironment.Storage.ResourceIdToken,
                    EnvironmentVariables = environmentVariables,
                },
                logger);
        }

        private class AllocateComputeResult
        {
            public Dictionary<string, string> EnvironmentVariables { get; set; }
        }
    }
}
