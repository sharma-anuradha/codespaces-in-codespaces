// <copyright file="CloudEnvironmentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repositories;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareAuthentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <inheritdoc/>
    public class CloudEnvironmentManager : ICloudEnvironmentManager
    {
        private const int PersistentSessionExpiresInDays = 30;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEnvironmentManager"/> class.
        /// </summary>
        /// <param name="cloudEnvironmentRepository">The cloud environment repository.</param>
        /// <param name="resourceBrokerHttpClient">The resource broker client.</param>
        /// <param name="workspaceRepository">The Live Share workspace repository.</param>
        /// <param name="planManager">The plan manager.</param>
        /// <param name="authRepository">The Live Share authentication repository.</param>
        /// <param name="billingEventManager">The billing event manager.</param>
        /// <param name="environmentManagerSettings">The environment manager settings.</param>
        public CloudEnvironmentManager(
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IResourceBrokerResourcesHttpContract resourceBrokerHttpClient,
            IWorkspaceRepository workspaceRepository,
            IPlanManager planManager,
            IAuthRepository authRepository,
            IBillingEventManager billingEventManager,
            EnvironmentManagerSettings environmentManagerSettings)
        {
            CloudEnvironmentRepository = Requires.NotNull(cloudEnvironmentRepository, nameof(cloudEnvironmentRepository));
            WorkspaceRepository = Requires.NotNull(workspaceRepository, nameof(workspaceRepository));
            ResourceBrokerClient = Requires.NotNull(resourceBrokerHttpClient, nameof(resourceBrokerHttpClient));
            PlanManager = Requires.NotNull(planManager, nameof(planManager));
            AuthRepository = Requires.NotNull(authRepository, nameof(authRepository));
            BillingEventManager = Requires.NotNull(billingEventManager, nameof(billingEventManager));
            EnvironmentManagerSettings = Requires.NotNull(environmentManagerSettings, nameof(environmentManagerSettings));
        }

        private ICloudEnvironmentRepository CloudEnvironmentRepository { get; }

        private IWorkspaceRepository WorkspaceRepository { get; }

        private IAuthRepository AuthRepository { get; }

        private IResourceBrokerResourcesHttpContract ResourceBrokerClient { get; }

        private IPlanManager PlanManager { get; }

        private IBillingEventManager BillingEventManager { get; }

        private EnvironmentManagerSettings EnvironmentManagerSettings { get; }

        /// <inheritdoc/>
        public async Task<CloudEnvironmentServiceResult> ShutdownEnvironmentAsync(
            string id,
            string currentUserId,
            IDiagnosticsLogger logger)
        {
            var duration = logger.StartDuration();

            try
            {
                Requires.NotNull(id, nameof(id));
                Requires.NotNull(logger, nameof(logger));

                // Validate input
                UnauthorizedUtil.IsRequired(currentUserId);

                // Check if the environment exists.
                var cloudEnvironment = await CloudEnvironmentRepository.GetAsync(id, logger);
                if (cloudEnvironment == null)
                {
                    return new CloudEnvironmentServiceResult
                    {
                        CloudEnvironment = null,
                        ErrorCode = Contracts.ErrorCodes.EnvironmentDoesNotExist,
                        HttpStatusCode = StatusCodes.Status404NotFound,
                    };
                }

                // Static Environment
                if (cloudEnvironment.Type == CloudEnvironmentType.StaticEnvironment)
                {
                    logger.AddDuration(duration)
                        .AddCloudEnvironment(cloudEnvironment)
                        .LogInfo(GetType().FormatLogMessage(nameof(ShutdownEnvironmentAsync)));

                    return new CloudEnvironmentServiceResult
                    {
                        CloudEnvironment = cloudEnvironment,
                        ErrorCode = Contracts.ErrorCodes.ShutdownStaticEnvironment,
                        HttpStatusCode = StatusCodes.Status400BadRequest,
                    };
                }

                if (cloudEnvironment.State == CloudEnvironmentState.Shutdown)
                {
                    return new CloudEnvironmentServiceResult
                    {
                        CloudEnvironment = cloudEnvironment,
                        HttpStatusCode = StatusCodes.Status200OK,
                    };
                }

                if (cloudEnvironment.State != CloudEnvironmentState.Available)
                {
                    // If the environment is not in an available state during shutdown,
                    // force clean the environment details, to put it in a recoverable state.
                    await ForceEnvironmentShutdownAsync(id, logger);
                }
                else
                {
                    await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.ShuttingDown, CloudEnvironmentStateUpdateReasons.ShutdownEnvironment, logger);

                    // Update the database state.
                    await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, logger);

                    // Start the cleanup operation to shutdown environment.
                    await ResourceBrokerClient.CleanupResourceAsync(cloudEnvironment.Compute.ResourceId, cloudEnvironment.Id, logger);
                }

                logger.AddDuration(duration)
                    .AddCloudEnvironment(cloudEnvironment)
                    .LogInfo(GetType().FormatLogMessage(nameof(ShutdownEnvironmentAsync)));

                return new CloudEnvironmentServiceResult
                {
                    CloudEnvironment = cloudEnvironment,
                    HttpStatusCode = StatusCodes.Status200OK,
                };
            }
            catch (Exception ex)
            {
                logger
                    .AddDuration(duration)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(ShutdownEnvironmentAsync)), ex.Message);

                throw;
            }
        }

        /// <inheritdoc/>
        public async Task ShutdownEnvironmentCallbackAsync(
            string id,
            IDiagnosticsLogger logger)
        {
            var duration = logger.StartDuration();

            try
            {
                Requires.NotNull(id, nameof(id));
                Requires.NotNull(logger, nameof(logger));

                // Check if the environment exists.
                var cloudEnvironment = await CloudEnvironmentRepository.GetAsync(id, logger);
                if (cloudEnvironment == null)
                {
                    return;
                }

                // Static Environment
                if (cloudEnvironment.Type == CloudEnvironmentType.StaticEnvironment)
                {
                    logger.AddDuration(duration)
                        .AddCloudEnvironment(cloudEnvironment)
                        .LogInfo(GetType().FormatLogMessage(nameof(ShutdownEnvironmentCallbackAsync)));

                    return;
                }

                if (cloudEnvironment.State == CloudEnvironmentState.ShuttingDown)
                {
                    await ForceEnvironmentShutdownAsync(id, logger);

                    logger.AddDuration(duration)
                        .AddCloudEnvironment(cloudEnvironment)
                        .LogInfo(GetType().FormatLogMessage(nameof(ShutdownEnvironmentCallbackAsync)));
                }
            }
            catch (Exception ex)
            {
                logger
                    .AddDuration(duration)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(ShutdownEnvironmentCallbackAsync)), ex.Message);

                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<CloudEnvironmentServiceResult> StartEnvironmentAsync(
            string id,
            Uri serviceUri,
            string callbackUriFormat,
            string currentUserId,
            string accessToken,
            IDiagnosticsLogger logger)
        {
            var duration = logger.StartDuration();

            try
            {
                Requires.NotNull(id, nameof(id));
                Requires.NotNull(logger, nameof(logger));

                // Validate input
                UnauthorizedUtil.IsRequired(currentUserId);
                UnauthorizedUtil.IsRequired(accessToken);

                var cascadeToken = await AuthRepository.ExchangeToken(accessToken);
                UnauthorizedUtil.IsRequired(cascadeToken);

                // Check if the environment exists.
                var cloudEnvironment = await CloudEnvironmentRepository.GetAsync(id, logger);
                if (cloudEnvironment == null)
                {
                    return new CloudEnvironmentServiceResult
                    {
                        CloudEnvironment = null,
                        ErrorCode = Contracts.ErrorCodes.EnvironmentDoesNotExist,
                        HttpStatusCode = StatusCodes.Status404NotFound,
                    };
                }

                // Static Environment
                if (cloudEnvironment.Type == CloudEnvironmentType.StaticEnvironment)
                {
                    logger.AddDuration(duration)
                        .AddCloudEnvironment(cloudEnvironment)
                        .LogInfo(GetType().FormatLogMessage(nameof(StartEnvironmentAsync)));

                    return new CloudEnvironmentServiceResult
                    {
                        CloudEnvironment = cloudEnvironment,
                        ErrorCode = Contracts.ErrorCodes.StartStaticEnvironment,
                        HttpStatusCode = StatusCodes.Status400BadRequest,
                    };
                }

                if (cloudEnvironment.State == CloudEnvironmentState.Starting ||
                    cloudEnvironment.State == CloudEnvironmentState.Available)
                {
                    return new CloudEnvironmentServiceResult
                    {
                        CloudEnvironment = cloudEnvironment,
                        HttpStatusCode = StatusCodes.Status200OK,
                    };
                }

                if (cloudEnvironment.State != CloudEnvironmentState.Shutdown)
                {
                    return new CloudEnvironmentServiceResult
                    {
                        CloudEnvironment = null,
                        ErrorCode = Contracts.ErrorCodes.EnvironmentNotShutdown,
                        HttpStatusCode = StatusCodes.Status400BadRequest,
                    };
                }

                var connectionSessionId = cloudEnvironment.Connection?.ConnectionSessionId;
                if (!string.IsNullOrWhiteSpace(connectionSessionId))
                {
                    // Delete the previous liveshare session from database.
                    // Do not block start process on delete of old workspace from liveshare db.
                    var _ = Task.Run(() => WorkspaceRepository.DeleteAsync(connectionSessionId));
                    cloudEnvironment.Connection = null;
                }

                // Allocate Compute
                try
                {
                    cloudEnvironment.Compute = await AllocateComputeAsync(cloudEnvironment, logger);
                }
                catch (Exception ex) when (ex is RemoteInvocationException || ex is HttpResponseStatusException)
                {
                    logger.AddDuration(duration)
                        .AddCloudEnvironment(cloudEnvironment)
                        .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(StartEnvironmentAsync)), ex.Message);

                    return new CloudEnvironmentServiceResult
                    {
                        ErrorCode = Contracts.ErrorCodes.UnableToAllocateResourcesWhileStarting,
                        HttpStatusCode = StatusCodes.Status503ServiceUnavailable,
                    };
                }

                // Create the Live Share workspace
                cloudEnvironment.Connection = await CreateWorkspace(CloudEnvironmentType.CloudEnvironment, cloudEnvironment.Id, cloudEnvironment.Compute.ResourceId, logger);
                if (string.IsNullOrWhiteSpace(cloudEnvironment.Connection.ConnectionSessionId))
                {
                    logger.AddDuration(duration)
                        .AddCloudEnvironment(cloudEnvironment)
                        .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(StartEnvironmentAsync)), "Could not create the cloud environment workspace session.");
                    return new CloudEnvironmentServiceResult
                    {
                        ErrorCode = Contracts.ErrorCodes.UnableToAllocateResourcesWhileStarting,
                        HttpStatusCode = StatusCodes.Status503ServiceUnavailable,
                    };
                }

                await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Starting, CloudEnvironmentStateUpdateReasons.StartEnvironment, logger);
                await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, logger);

                // Kick off start-compute before returning.
                var callbackUri = new Uri(string.Format(callbackUriFormat, cloudEnvironment.Id));
                await StartCompute(cloudEnvironment, serviceUri, callbackUri, accessToken, cascadeToken, logger, null);

                logger.AddDuration(duration)
                    .AddCloudEnvironment(cloudEnvironment)
                    .LogInfo(GetType().FormatLogMessage(nameof(StartEnvironmentAsync)));

                return new CloudEnvironmentServiceResult
                {
                    CloudEnvironment = cloudEnvironment,
                    HttpStatusCode = StatusCodes.Status200OK,
                };
            }
            catch (Exception ex)
            {
                logger
                    .AddDuration(duration)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(StartEnvironmentAsync)), ex.Message);

                try
                {
                    await ShutdownEnvironmentAsync(id, currentUserId, logger);
                }
                catch (Exception ex2)
                {
                    throw new AggregateException(GetType().FormatLogErrorMessage(nameof(StartEnvironmentAsync)), ex, ex2);
                }

                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<CloudEnvironmentServiceResult> CreateEnvironmentAsync(
            CloudEnvironment cloudEnvironment,
            CloudEnvironmentOptions cloudEnvironmentOptions,
            Uri serviceUri,
            string callbackUriFormat,
            string currentUserId,
            string currentUserProviderId,
            string accessToken,
            IDiagnosticsLogger logger)
        {
            var duration = logger.StartDuration();

            var result = new CloudEnvironmentServiceResult()
            {
                ErrorCode = Contracts.ErrorCodes.Unknown,
                HttpStatusCode = StatusCodes.Status409Conflict,
            };

            try
            {
                Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
                Requires.NotNull(cloudEnvironmentOptions, nameof(cloudEnvironmentOptions));
                Requires.NotNull(logger, nameof(logger));

                // Validate input
                UnauthorizedUtil.IsRequired(currentUserId);
                UnauthorizedUtil.IsRequired(currentUserProviderId);
                UnauthorizedUtil.IsRequired(accessToken);

                var cascadeToken = await AuthRepository.ExchangeToken(accessToken);
                UnauthorizedUtil.IsRequired(cascadeToken);

                ValidationUtil.IsRequired(cloudEnvironment.SkuName, nameof(cloudEnvironment.SkuName));
                ValidationUtil.IsTrue(
                    cloudEnvironment.Location != default,
                    "Location is required");
                ValidationUtil.IsRequired(cloudEnvironment.PlanId, nameof(CloudEnvironment.PlanId));

                // Validate that the specified plan ID is well-formed.
                ValidationUtil.IsTrue(
                    VsoPlanInfo.TryParse(cloudEnvironment.PlanId, out var plan),
                    $"Invalid plan ID: {cloudEnvironment.PlanId}");

                // Validate the plan exists (and lookup the plan details).
                plan.Location = cloudEnvironment.Location;
                var planDetails = (await PlanManager.GetAsync(plan, logger)).VsoPlan;
                if (planDetails == null)
                {
                    throw new ArgumentException($"Plan not found.", nameof(cloudEnvironment.PlanId));
                }

                // Validate against existing environments.
                var environments = await CloudEnvironmentRepository.GetWhereAsync(
                    (env) => env.PlanId == cloudEnvironment.PlanId, logger);
                if (environments.Any((env) => string.Equals(
                    env.FriendlyName, cloudEnvironment.FriendlyName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    result.ErrorCode = Contracts.ErrorCodes.EnvironmentNameAlreadyExists;
                    result.HttpStatusCode = StatusCodes.Status409Conflict;
                    return result;
                }

                // Validate the calling user is the owner of the the plan (if the plan has an owner).
                // Match on provider ID instead of profile ID because clients dont have
                // the profile ID when the create the plan resource via ARM.
                UnauthorizedUtil.IsTrue(
                    planDetails.UserId == null || currentUserProviderId == planDetails.UserId);

                // TODO: Validate the plan & subscription are in a good state?

                // Check for quota on # of environments per plan
                var totalEnvironments = await ListEnvironmentsAsync(userId: null, string.Empty, plan.ResourceId, logger);
                if (totalEnvironments.Count() >= await EnvironmentManagerSettings.MaxEnvironmentsPerPlanAsync(plan.Subscription, logger))
                {
                    logger.AddDuration(duration)
                        .AddCloudEnvironment(cloudEnvironment)
                        .LogInfo(GetType().FormatLogErrorMessage(nameof(CreateEnvironmentAsync)));

                    result.ErrorCode = Contracts.ErrorCodes.ExceededQuota;
                    result.HttpStatusCode = StatusCodes.Status403Forbidden;
                    return result;
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

                    if (cloudEnvironment.Seed == default || cloudEnvironment.Seed.SeedType != SeedType.StaticEnvironment)
                    {
                        cloudEnvironment.Seed = new SeedInfo { SeedType = SeedType.StaticEnvironment };
                    }

                    cloudEnvironment.SkuName = StaticEnvironmentSku.Name;

                    // Environments must be initialized in Created state. But (at least for now) new environments immediately transition to Provisioning state.
                    await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Created, CloudEnvironmentStateUpdateReasons.CreateEnvironment, logger);
                    await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Provisioning, CloudEnvironmentStateUpdateReasons.CreateEnvironment, logger);

                    cloudEnvironment = await CloudEnvironmentRepository.CreateAsync(cloudEnvironment, logger);

                    logger.AddDuration(duration)
                        .AddCloudEnvironment(cloudEnvironment)
                        .LogInfo(GetType().FormatLogMessage(nameof(CreateEnvironmentAsync)));

                    result.CloudEnvironment = cloudEnvironment;
                    result.HttpStatusCode = StatusCodes.Status200OK;
                    return result;
                }

                // Allocate Storage and Compute
                try
                {
                    var allocationResult = await AllocateComputeAndStorageAsync(cloudEnvironment, logger);
                    cloudEnvironment.Storage = allocationResult.Storage;
                    cloudEnvironment.Compute = allocationResult.Compute;
                }
                catch (Exception ex) when (ex is RemoteInvocationException || ex is HttpResponseStatusException)
                {
                    logger.AddDuration(duration)
                        .AddCloudEnvironment(cloudEnvironment)
                        .LogException(GetType().FormatLogErrorMessage(nameof(CreateEnvironmentAsync)), ex);

                    result.ErrorCode = Contracts.ErrorCodes.UnableToAllocateResources;
                    result.HttpStatusCode = StatusCodes.Status503ServiceUnavailable;
                    return result;
                }

                // Create the Live Share workspace
                cloudEnvironment.Connection = await CreateWorkspace(CloudEnvironmentType.CloudEnvironment, cloudEnvironment.Id, cloudEnvironment.Compute.ResourceId, logger);
                if (string.IsNullOrWhiteSpace(cloudEnvironment.Connection.ConnectionSessionId))
                {
                    logger.AddDuration(duration)
                        .AddCloudEnvironment(cloudEnvironment)
                        .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(CreateEnvironmentAsync)), "Could not create the cloud environment workspace session.");
                    result.ErrorCode = Contracts.ErrorCodes.UnableToAllocateResources;
                    result.HttpStatusCode = StatusCodes.Status503ServiceUnavailable;
                    return result;
                }

                // Create the cloud environment record in the provisioning state -- before starting.
                // This avoids a race condition where the record doesn't exist but the callback could be invoked.
                // Highly unlikely, but still...
                await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Provisioning, CloudEnvironmentStateUpdateReasons.CreateEnvironment, logger);

                cloudEnvironment = await CloudEnvironmentRepository.CreateAsync(cloudEnvironment, logger);

                // Kick off start-compute before returning.
                var callbackUri = new Uri(string.Format(callbackUriFormat, cloudEnvironment.Id));
                await StartCompute(cloudEnvironment, serviceUri, callbackUri, accessToken, cascadeToken, logger, cloudEnvironmentOptions);

                logger.AddDuration(duration)
                    .AddCloudEnvironment(cloudEnvironment)
                    .LogInfo(GetType().FormatLogMessage(nameof(CreateEnvironmentAsync)));

                result.CloudEnvironment = cloudEnvironment;
                result.HttpStatusCode = StatusCodes.Status200OK;
                return result;
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .AddCloudEnvironment(cloudEnvironment)
                    .LogException(GetType().FormatLogErrorMessage(nameof(CreateEnvironmentAsync)), ex);

                if (cloudEnvironment.Id != null)
                {
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
                await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Available, CloudEnvironmentStateUpdateReasons.EnvironmentCallback, logger);
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

                UnauthorizedUtil.IsTrue(currentUserId == cloudEnvironment.OwnerId);

                // Update the new state before returning.
                var originalState = cloudEnvironment.State;
                var newState = originalState;

                // Check for an unavailable environment
                switch (originalState)
                {
                    // Remain in provisioning state until _callback is invoked.
                    case CloudEnvironmentState.Provisioning:

                        // Timeout if environment has stayed in provisioning state for more than an hour
                        var timeInProvisioningStateInMin = (DateTime.UtcNow - cloudEnvironment.LastStateUpdated).TotalMinutes;
                        if (timeInProvisioningStateInMin > 60)
                        {
                            newState = CloudEnvironmentState.Failed;
                        }

                        logger.AddCloudEnvironment(cloudEnvironment)
                              .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(GetEnvironmentAsync)), $"Marking environment creation failed with timeout. Time in provisioning state {timeInProvisioningStateInMin} minutes ");
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
                    await SetEnvironmentStateAsync(cloudEnvironment, newState, CloudEnvironmentStateUpdateReasons.GetEnvironment, logger);
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
        public async Task<IEnumerable<CloudEnvironment>> ListEnvironmentsAsync(
            string userId,
            string environmentName,
            string planId,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(logger, nameof(logger));

            if (userId == null)
            {
                Requires.NotNull(planId, nameof(planId));

                if (!string.IsNullOrEmpty(environmentName))
                {
                    return await CloudEnvironmentRepository.GetWhereAsync(
                        (cloudEnvironment) => cloudEnvironment.PlanId == planId &&
                            cloudEnvironment.FriendlyName == environmentName.Trim(),
                        logger);
                }
                else
                {
                    return await CloudEnvironmentRepository.GetWhereAsync(
                        (cloudEnvironment) => cloudEnvironment.PlanId == planId, logger);
                }
            }
            else if (planId == null)
            {
                if (!string.IsNullOrEmpty(environmentName))
                {
                    Requires.NotNull(userId, nameof(userId));
                    return await CloudEnvironmentRepository.GetWhereAsync(
                        (cloudEnvironment) => cloudEnvironment.OwnerId == userId &&
                            cloudEnvironment.FriendlyName == environmentName.Trim(),
                        logger);
                }
                else
                {
                    return await CloudEnvironmentRepository.GetWhereAsync(
                        (cloudEnvironment) => cloudEnvironment.OwnerId == userId, logger);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(environmentName))
                {
                    Requires.NotNull(userId, nameof(userId));
                    return await CloudEnvironmentRepository.GetWhereAsync(
                        (cloudEnvironment) => cloudEnvironment.OwnerId == userId &&
                            cloudEnvironment.PlanId == planId &&
                            cloudEnvironment.FriendlyName == environmentName.Trim(),
                        logger);
                }
                else
                {
                    return await CloudEnvironmentRepository.GetWhereAsync(
                        (cloudEnvironment) => cloudEnvironment.OwnerId == userId &&
                            cloudEnvironment.PlanId == planId,
                        logger);
                }
            }
        }

        /// <inheritdoc/>
        public async Task<CloudEnvironment> GetEnvironmentByIdAsync(
            string id,
            IDiagnosticsLogger logger)
        {
            ValidationUtil.IsRequired(id);
            Requires.NotNull(logger, nameof(logger));
            return await CloudEnvironmentRepository.GetAsync(id, logger);
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

            await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Deleted, CloudEnvironmentStateUpdateReasons.DeleteEnvironment, logger);

            if (cloudEnvironment.Type == CloudEnvironmentType.CloudEnvironment)
            {
                var storageIdToken = cloudEnvironment.Storage?.ResourceId;
                if (storageIdToken != null)
                {
                    await logger.OperationScopeAsync(
                        $"{this.GetType().GetLogMessageBaseName()}_delete_storage_async",
                        async (childLogger) =>
                        {
                            childLogger.FluentAddBaseValue(nameof(id), id)
                            .FluentAddBaseValue(nameof(storageIdToken), storageIdToken.Value);
                            await ResourceBrokerClient.DeleteResourceAsync(storageIdToken.Value, childLogger);
                        },
                        swallowException: true);
                }

                var computeIdToken = cloudEnvironment.Compute?.ResourceId;
                if (computeIdToken != null)
                {
                    await logger.OperationScopeAsync(
                       $"{this.GetType().GetLogMessageBaseName()}_delete_compute_async",
                       async (childLogger) =>
                       {
                           childLogger.FluentAddBaseValue(nameof(id), id)
                            .FluentAddBaseValue(nameof(computeIdToken), computeIdToken.Value);
                           await ResourceBrokerClient.DeleteResourceAsync(computeIdToken.Value, childLogger);
                       },
                       swallowException: true);
                }
            }

            if (cloudEnvironment.Connection?.ConnectionSessionId != null)
            {
                await logger.OperationScopeAsync(
                    $"{this.GetType().GetLogMessageBaseName()}_delete_workspace_async",
                    async (childLogger) =>
                    {
                        childLogger.FluentAddBaseValue(nameof(id), id)
                            .FluentAddBaseValue("ConnectionSessionId", cloudEnvironment.Connection?.ConnectionSessionId);
                        await WorkspaceRepository.DeleteAsync(cloudEnvironment.Connection.ConnectionSessionId);
                    },
                    swallowException: true);
            }

            await CloudEnvironmentRepository.DeleteAsync(id, logger);

            return true;
        }

        /// <inheritdoc/>
        public async Task<CloudEnvironment> UpdateEnvironmentAsync(CloudEnvironment cloudEnvironment, CloudEnvironmentState newState, string reason, IDiagnosticsLogger logger)
        {
            cloudEnvironment.Updated = DateTime.UtcNow;
            if (newState != default && newState != cloudEnvironment.State)
            {
                await SetEnvironmentStateAsync(cloudEnvironment, newState, reason, logger);
            }

            return await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, logger);
        }

        /// <inheritdoc/>
        public async Task ForceEnvironmentShutdownAsync(string id, IDiagnosticsLogger logger)
        {
            Requires.NotNull(id, nameof(id));
            Requires.NotNull(logger, nameof(logger));

            await logger.OperationScopeAsync(
                $"{this.GetType().GetLogMessageBaseName()}_force_shutdown_async",
                async (childLogger) =>
                {
                    var cloudEnvironment = await GetEnvironmentByIdAsync(id, logger);
                    if (cloudEnvironment == null)
                    {
                        return;
                    }

                    await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Shutdown, CloudEnvironmentStateUpdateReasons.ForceEnvironmentShutdown, logger);
                    var computeIdToken = cloudEnvironment.Compute?.ResourceId;
                    cloudEnvironment.Compute = null;

                    // Update the database state.
                    await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, logger);

                    // Delete the allocated resources.
                    if (computeIdToken != null)
                    {
                        await ResourceBrokerClient.DeleteResourceAsync(computeIdToken.Value, logger);
                    }
                }, swallowException: true);
        }

        private async Task SetEnvironmentStateAsync(
            CloudEnvironment cloudEnvironment,
            CloudEnvironmentState state,
            string reason,
            IDiagnosticsLogger logger)
        {
            var oldState = cloudEnvironment.State;
            var oldStateUpdated = cloudEnvironment.LastStateUpdated;
            logger.AddCloudEnvironment(cloudEnvironment)
                  .FluentAddValue("OldState", oldState)
                  .FluentAddValue("OldStateUpdated", oldStateUpdated);

            VsoPlanInfo plan;
            if (cloudEnvironment.PlanId == default)
            {
                // Use a temporary plan if the environment doesn't have one.
                // TODO: Remove this; make the plan required after clients are updated to supply it.
                plan = new VsoPlanInfo
                {
                    Subscription = Guid.Empty.ToString(),
                    ResourceGroup = "none",
                    Name = "none",
                };
            }
            else
            {
                Requires.Argument(
                    VsoPlanInfo.TryParse(cloudEnvironment.PlanId, out plan),
                    nameof(cloudEnvironment.PlanId),
                    "Invalid plan ID");

                plan.Location = cloudEnvironment.Location;
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
                plan, environment, BillingEventTypes.EnvironmentStateChange, stateChange, logger);

            cloudEnvironment.State = state;
            cloudEnvironment.LastStateUpdateReason = reason;
            cloudEnvironment.LastStateUpdated = DateTime.UtcNow;

            logger.LogInfo(GetType().FormatLogMessage(nameof(SetEnvironmentStateAsync)));
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

        private async Task<ResourceAllocation> AllocateComputeAsync(
            CloudEnvironment cloudEnvironment,
            IDiagnosticsLogger logger)
        {
            var computeRequest = new CreateResourceRequestBody
            {
                Type = ResourceType.ComputeVM,
                SkuName = cloudEnvironment.SkuName,
                Location = cloudEnvironment.Location,
            };

            var inputRequest = new List<CreateResourceRequestBody> { computeRequest };

            var resultResponse = await ResourceBrokerClient.CreateResourceSetAsync(
                inputRequest, logger);

            if (resultResponse != null && resultResponse.Count() == inputRequest.Count)
            {
                var computeResponse = resultResponse.Where(x => x.Type == ResourceType.ComputeVM).FirstOrDefault();
                if (computeResponse != null)
                {
                    var computeResult = new ResourceAllocation
                    {
                        ResourceId = computeResponse.ResourceId,
                        SkuName = computeResponse.SkuName,
                        Location = computeResponse.Location,
                        Created = computeResponse.Created,
                    };

                    return computeResult;
                }
            }

            throw new InvalidOperationException("Allocate result for Compute and Storage was invalid.");
        }

        private async Task<(ResourceAllocation Compute, ResourceAllocation Storage)> AllocateComputeAndStorageAsync(
            CloudEnvironment cloudEnvironment,
            IDiagnosticsLogger logger)
        {
            var computeRequest = new CreateResourceRequestBody
            {
                Type = ResourceType.ComputeVM,
                SkuName = cloudEnvironment.SkuName,
                Location = cloudEnvironment.Location,
            };

            var storageRequest = new CreateResourceRequestBody
            {
                Type = ResourceType.StorageFileShare,
                SkuName = cloudEnvironment.SkuName,
                Location = cloudEnvironment.Location,
            };

            var inputRequest = new List<CreateResourceRequestBody> { computeRequest, storageRequest };

            var resultResponse = await ResourceBrokerClient.CreateResourceSetAsync(
                inputRequest, logger);

            if (resultResponse != null && resultResponse.Count() == inputRequest.Count)
            {
                var computeResponse = resultResponse.Where(x => x.Type == ResourceType.ComputeVM).FirstOrDefault();
                var storageResponse = resultResponse.Where(x => x.Type == ResourceType.StorageFileShare).FirstOrDefault();
                if (computeResponse != null && storageResponse != null)
                {
                    var computeResult = new ResourceAllocation
                    {
                        ResourceId = computeResponse.ResourceId,
                        SkuName = computeResponse.SkuName,
                        Location = computeResponse.Location,
                        Created = computeResponse.Created,
                    };
                    var stroageResult = new ResourceAllocation
                    {
                        ResourceId = storageResponse.ResourceId,
                        SkuName = storageResponse.SkuName,
                        Location = storageResponse.Location,
                        Created = storageResponse.Created,
                    };

                    return (computeResult, stroageResult);
                }
            }

            throw new InvalidOperationException("Allocate result for Compute and Storage was invalid.");
        }

        private async Task StartCompute(
            CloudEnvironment cloudEnvironment,
            Uri serviceUri,
            Uri callbackUri,
            string accessToken,
            string cascadeToken,
            IDiagnosticsLogger logger,
            CloudEnvironmentOptions cloudEnvironmentOptions)
        {
            Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
            Requires.NotNull(cloudEnvironment.Compute, nameof(cloudEnvironment.Compute));
            Requires.NotNull(cloudEnvironment.Storage, nameof(cloudEnvironment.Storage));
            Requires.NotEmpty(cloudEnvironment.Compute.ResourceId, $"{nameof(cloudEnvironment.Compute)}.{nameof(cloudEnvironment.Compute.ResourceId)}");
            Requires.NotEmpty(cloudEnvironment.Storage.ResourceId, $"{nameof(cloudEnvironment.Storage)}.{nameof(cloudEnvironment.Storage.ResourceId)}");
            Requires.NotNull(callbackUri, nameof(callbackUri));
            Requires.Argument(callbackUri.IsAbsoluteUri, nameof(callbackUri), "Must be an absolute URI.");
            Requires.NotNullOrEmpty(accessToken, nameof(accessToken));
            Requires.NotNullOrEmpty(cascadeToken, nameof(cascadeToken));

            // Construct the start-compute environment variables
            var environmentVariables = EnvironmentVariableGenerator.Generate(
                cloudEnvironment,
                serviceUri,
                callbackUri,
                accessToken,
                cascadeToken,
                cloudEnvironmentOptions);

            await ResourceBrokerClient.StartComputeAsync(
                cloudEnvironment.Compute.ResourceId,
                new StartComputeRequestBody
                {
                    StorageResourceId = cloudEnvironment.Storage.ResourceId,
                    EnvironmentVariables = environmentVariables,
                },
                logger);
        }
    }
}
