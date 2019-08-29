// <copyright file="CloudEnvironmentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Accounts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
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
        /// <param name="accountManager">The account manager.</param>
        /// <param name="billingEventManager">The billing event manager.</param>
        public CloudEnvironmentManager(
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IResourceBrokerResourcesHttpContract resourceBrokerHttpClient,
            IWorkspaceRepository workspaceRepository,
            IAccountManager accountManager,
            IBillingEventManager billingEventManager)
        {
            CloudEnvironmentRepository = Requires.NotNull(cloudEnvironmentRepository, nameof(cloudEnvironmentRepository));
            WorkspaceRepository = Requires.NotNull(workspaceRepository, nameof(workspaceRepository));
            ResourceBrokerClient = Requires.NotNull(resourceBrokerHttpClient, nameof(resourceBrokerHttpClient));
            AccountManager = Requires.NotNull(accountManager, nameof(accountManager));
            BillingEventManager = Requires.NotNull(billingEventManager, nameof(billingEventManager));
        }

        private ICloudEnvironmentRepository CloudEnvironmentRepository { get; }

        private IWorkspaceRepository WorkspaceRepository { get; }

        private IResourceBrokerResourcesHttpContract ResourceBrokerClient { get; }

        private IAccountManager AccountManager { get; }

        private IBillingEventManager BillingEventManager { get; }

        /// <inheritdoc/>
        public async Task<CloudEnvironment> CreateEnvironmentAsync(
            CloudEnvironment cloudEnvironment,
            CloudEnvironmentOptions cloudEnvironmentOptions,
            string callbackUriFormat,
            string currentUserId,
            string accessToken,
            IDiagnosticsLogger logger)
        {
            var duration = logger.StartDuration();

            try
            {
                Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
                Requires.NotNull(cloudEnvironmentOptions, nameof(cloudEnvironmentOptions));
                Requires.NotNull(logger, nameof(logger));

                // Validate input
                UnauthorizedUtil.IsRequired(currentUserId);
                UnauthorizedUtil.IsRequired(accessToken);
                ValidationUtil.IsRequired(cloudEnvironment.SkuName, nameof(cloudEnvironment.SkuName));
                ValidationUtil.IsTrue(
                    cloudEnvironment.Location != default,
                    "Location is required");

                // Validate against existing environments.
                var environments = await CloudEnvironmentRepository.GetWhereAsync((env) => env.OwnerId == currentUserId, logger);
                ValidationUtil.IsTrue(
                    !environments.Any((env) => string.Equals(env.FriendlyName, cloudEnvironment.FriendlyName, StringComparison.InvariantCultureIgnoreCase)),
                    $"An environment with the friendly name already exists: {cloudEnvironment.FriendlyName}");
                ValidationUtil.IsTrue(
                    environments.Count() < CloudEnvironmentQuota,
                    $"You have reached the limit of {CloudEnvironmentQuota} environments");

                // TODO: Make AccountId required after clients are updated to supply it.
                if (cloudEnvironment.AccountId != null)
                {
                    // Validate that the specified account ID is well-formed.
                    ValidationUtil.IsTrue(
                        VsoAccountInfo.TryParse(cloudEnvironment.AccountId, out var account),
                        $"Invalid account ID: {cloudEnvironment.AccountId}");

                    // Validate the account exists (and lookup the account details).
                    account.Location = cloudEnvironment.Location;
                    var accountDetails = await AccountManager.GetAsync(account, logger);
                    if (accountDetails == null)
                    {
                        throw new ArgumentException($"Account not found.", nameof(cloudEnvironment.AccountId));
                    }

                    // TODO: Validate the calling user has contribute access to the account?
                    // TODO: Validate the account & subscription are in a good state?
                    // TODO: Check for quota on # of environments per account?
                }

                // Setup
                cloudEnvironment.Id = Guid.NewGuid().ToString();
                cloudEnvironment.OwnerId = currentUserId;
                cloudEnvironment.Created = cloudEnvironment.Updated = DateTime.UtcNow;

                // Static Environment
                if (cloudEnvironment.Type == CloudEnvironmentType.StaticEnvironment)
                {
                    if (cloudEnvironment.Connection is null)
                    {
                        cloudEnvironment.Connection = new ConnectionInfo();
                    }

                    if (string.IsNullOrWhiteSpace(cloudEnvironment.Connection.ConnectionSessionId))
                    {
                        cloudEnvironment.Connection = await CreateWorkspace(CloudEnvironmentType.StaticEnvironment, cloudEnvironment.Id, Guid.Empty, logger);
                    }

                    // Environments must be initialized in Created state. But (at least for now) new environments immediately transition to Provisioning state.
                    await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Created, logger);
                    await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Provisioning, logger);

                    cloudEnvironment = await CloudEnvironmentRepository.CreateAsync(cloudEnvironment, logger);

                    logger.AddDuration(duration)
                        .AddCloudEnvironment(cloudEnvironment)
                        .LogInfo(GetType().FormatLogMessage(nameof(CreateEnvironmentAsync)));

                    return cloudEnvironment;
                }

                // Allocate Storage
                // TODO: What about cloudEnvironmentOptions.CreateFileShare
                cloudEnvironment.Storage = await AllocateStorage(cloudEnvironment, logger);

                // Allocate Compute
                cloudEnvironment.Compute = await AllocateCompute(cloudEnvironment, logger);

                // Create the Live Share workspace
                cloudEnvironment.Connection = await CreateWorkspace(CloudEnvironmentType.CloudEnvironment, cloudEnvironment.Id, cloudEnvironment.Compute.ResourceId, logger);
                if (string.IsNullOrWhiteSpace(cloudEnvironment.Connection.ConnectionSessionId))
                {
                    logger.AddDuration(duration)
                        .AddCloudEnvironment(cloudEnvironment)
                        .LogError(GetType().FormatLogErrorMessage(nameof(CreateEnvironmentAsync)));
                    throw new InvalidOperationException("Cloud not create the cloud environment workspace session.");
                }

                // Create the cloud environment record in the provisioining state -- before starting.
                // This avoids a race condition where the record doesn't exist but the callback could be invoked.
                // Highly unlikely, but still...
                await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Provisioning, logger);

                cloudEnvironment = await CloudEnvironmentRepository.CreateAsync(cloudEnvironment, logger);

                // Kick off start-compute before returning.
                var callbackUri = new Uri(string.Format(callbackUriFormat, cloudEnvironment.Id));
                await StartCompute(cloudEnvironment, callbackUri, accessToken, logger);

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
                    throw new AggregateException(GetType().FormatLogErrorMessage(nameof(CreateEnvironmentAsync)), ex, ex2);
                }

                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<CloudEnvironment> UpdateEnvironmentCallbackAsync(
            string id,
            EnvironmentRegistrationCallbackOptions options,
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
                await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Available, logger);
                cloudEnvironment.Updated = DateTime.UtcNow;
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

                // Update the new state before returning.
                var originalState = cloudEnvironment.State;
                var newState = originalState;

                // Check for an unavailable environment
                switch (originalState)
                {
                    // Remain in provisioining state until _callback is invoked.
                    case CloudEnvironmentState.Provisioning:
                        break;

                    // Swap between available and awaiting based on the workspace status
                    case CloudEnvironmentState.Available:
                    case CloudEnvironmentState.Awaiting:
                        var sessionId = cloudEnvironment.Connection?.ConnectionSessionId;
                        var workspace = await WorkspaceRepository.GetStatusAsync(sessionId);
                        if (workspace == null)
                        {
                            // In this case the workspace is deleted. There is no way of getting to an environment without it.
                            newState = CloudEnvironmentState.Unavailable;
                        }
                        else if (workspace.IsHostConnected.HasValue)
                        {
                            newState = workspace.IsHostConnected.Value ? CloudEnvironmentState.Available : CloudEnvironmentState.Awaiting;
                        }

                        break;
                }

                // Update the new state before returning.
                if (originalState != newState)
                {
                    await SetEnvironmentStateAsync(cloudEnvironment, newState, logger);
                    cloudEnvironment.Updated = DateTime.UtcNow;
                    cloudEnvironment = await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, logger);
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

            await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Deleted, logger);

            if (cloudEnvironment.Type == CloudEnvironmentType.CloudEnvironment)
            {
                var storageIdToken = cloudEnvironment.Storage?.ResourceId;
                if (storageIdToken != null)
                {
                    await ResourceBrokerClient.DeleteResourceAsync(storageIdToken.Value, logger);
                }

                var computeIdToken = cloudEnvironment.Compute?.ResourceId;
                if (computeIdToken != null)
                {
                    await ResourceBrokerClient.DeleteResourceAsync(computeIdToken.Value, logger);
                }
            }

            await WorkspaceRepository.DeleteAsync(cloudEnvironment.Connection.ConnectionSessionId);
            await CloudEnvironmentRepository.DeleteAsync(id, logger);

            return true;
        }

        private async Task<ConnectionInfo> CreateWorkspace(
            CloudEnvironmentType type,
            string cloudEnvironmentId,
            Guid computeIdToken,
            IDiagnosticsLogger logger)
        {
            var duration = logger.StartDuration();

            Requires.NotNullOrEmpty(cloudEnvironmentId, nameof(cloudEnvironmentId));

            var workspaceRequest = new WorkspaceRequest()
            {
                Name = cloudEnvironmentId.ToString(),
                ConnectionMode = ConnectionMode.Auto,
                AreAnonymousGuestsAllowed = false,
                ExpiresAt = DateTime.UtcNow.AddDays(PersistentSessionExpiresInDays),
            };

            var workspaceResponse = await WorkspaceRepository.CreateAsync(workspaceRequest);
            if (string.IsNullOrWhiteSpace(workspaceResponse.Id))
            {
                logger
                    .AddEnvironmentId(cloudEnvironmentId)
                    .LogError(GetType().FormatLogErrorMessage(nameof(CreateWorkspace)));
                return null;
            }

            return new ConnectionInfo
            {
                ConnectionComputeId = computeIdToken.ToString(),
                ConnectionComputeTargetId = type.ToString(),
                ConnectionSessionId = workspaceResponse.Id,
                ConnectionSessionPath = null,
            };
        }

        private async Task<ResourceAllocation> AllocateCompute(
            CloudEnvironment cloudEnvironment,
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

            return new ResourceAllocation
            {
                ResourceId = compute.ResourceId,
                SkuName = compute.SkuName,
                Location = compute.Location,
                Created = compute.Created,
            };
        }

        private async Task<ResourceAllocation> AllocateStorage(
            CloudEnvironment cloudEnvironment,
            IDiagnosticsLogger logger)
        {
            var storage = await ResourceBrokerClient.CreateResourceAsync(
                new CreateResourceRequestBody
                {
                    Type = ResourceType.StorageFileShare,
                    SkuName = cloudEnvironment.SkuName,
                    Location = cloudEnvironment.Location,
                },
                logger);

            return new ResourceAllocation
            {
                ResourceId = storage.ResourceId,
                SkuName = storage.SkuName,
                Location = storage.Location,
                Created = storage.Created,
            };
        }

        private async Task StartCompute(
            CloudEnvironment cloudEnvironment,
            Uri callbackUri,
            string accessToken,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
            Requires.NotNull(cloudEnvironment.Compute, nameof(cloudEnvironment.Compute));
            Requires.NotNull(cloudEnvironment.Storage, nameof(cloudEnvironment.Storage));
            Requires.NotEmpty(cloudEnvironment.Compute.ResourceId, $"{nameof(cloudEnvironment.Compute)}.{nameof(cloudEnvironment.Compute.ResourceId)}");
            Requires.NotEmpty(cloudEnvironment.Storage.ResourceId, $"{nameof(cloudEnvironment.Storage)}.{nameof(cloudEnvironment.Storage.ResourceId)}");
            Requires.NotNull(callbackUri, nameof(callbackUri));
            Requires.Argument(callbackUri.IsAbsoluteUri, nameof(callbackUri), "Must be an absolute URI.");
            Requires.NotNullOrEmpty(accessToken, nameof(accessToken));

            // Construct the start-compute environment variables
            var environmentVariables = EnvironmentVariableGenerator.Generate(
                cloudEnvironment,
                callbackUri,
                accessToken);

            await ResourceBrokerClient.StartComputeAsync(
                cloudEnvironment.Compute.ResourceId,
                new StartComputeRequestBody
                {
                    StorageResourceId = cloudEnvironment.Storage.ResourceId,
                    EnvironmentVariables = environmentVariables,
                },
                logger);
        }

        /// <summary>
        /// Updates the `State` property of an environment and emits a billing event
        /// to record the state change for billing purposes.
        /// </summary>
        private async Task SetEnvironmentStateAsync(
            CloudEnvironment cloudEnvironment,
            CloudEnvironmentState state,
            IDiagnosticsLogger logger)
        {
            var oldState = cloudEnvironment.State;

            VsoAccountInfo account;
            if (cloudEnvironment.AccountId == default)
            {
                // Use a temporary account if the environment doesn't have one.
                // TODO: Remove this; make the account required after clients are updated to supply it.
                account = new VsoAccountInfo
                {
                    Subscription = Guid.Empty.ToString(),
                    ResourceGroup = "none",
                    Name = "none",
                };
            }
            else
            {
                Requires.Argument(
                    VsoAccountInfo.TryParse(cloudEnvironment.AccountId, out account),
                    nameof(cloudEnvironment.AccountId),
                    "Invalid account ID");
            }

            var environment = new EnvironmentBillingInfo
            {
                Id = cloudEnvironment.Id,
                Name = cloudEnvironment.FriendlyName,
                UserId = cloudEnvironment.OwnerId,
                Sku = new Sku { Name = cloudEnvironment.SkuName, Tier = string.Empty },
            };

            var stateChange = new BillingStateChange
            {
                OldValue = (oldState == default ? CloudEnvironmentState.Created : oldState).ToString(),
                NewValue = state.ToString(),
            };

            await BillingEventManager.CreateEventAsync(
                account, environment, BillingEventTypes.EnvironmentStateChange, stateChange, logger);

            cloudEnvironment.State = state;
        }
    }
}
