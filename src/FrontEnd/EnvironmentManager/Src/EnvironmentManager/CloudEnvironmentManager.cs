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
        /// <param name="authRepository">The Live Share authentication repository.</param>
        /// <param name="billingEventManager">The billing event manager.</param>
        /// <param name="skuCatalog">The SKU catalog.</param>
        /// <param name="environmentManagerSettings">The environment manager settings.</param>
        public CloudEnvironmentManager(
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IResourceBrokerResourcesHttpContract resourceBrokerHttpClient,
            IWorkspaceRepository workspaceRepository,
            IAuthRepository authRepository,
            IBillingEventManager billingEventManager,
            ISkuCatalog skuCatalog,
            EnvironmentManagerSettings environmentManagerSettings)
        {
            CloudEnvironmentRepository = Requires.NotNull(cloudEnvironmentRepository, nameof(cloudEnvironmentRepository));
            WorkspaceRepository = Requires.NotNull(workspaceRepository, nameof(workspaceRepository));
            ResourceBrokerClient = Requires.NotNull(resourceBrokerHttpClient, nameof(resourceBrokerHttpClient));
            AuthRepository = Requires.NotNull(authRepository, nameof(authRepository));
            BillingEventManager = Requires.NotNull(billingEventManager, nameof(billingEventManager));
            SkuCatalog = skuCatalog;
            EnvironmentManagerSettings = Requires.NotNull(environmentManagerSettings, nameof(environmentManagerSettings));
        }

        private ICloudEnvironmentRepository CloudEnvironmentRepository { get; }

        private IWorkspaceRepository WorkspaceRepository { get; }

        private IAuthRepository AuthRepository { get; }

        private IResourceBrokerResourcesHttpContract ResourceBrokerClient { get; }

        private IBillingEventManager BillingEventManager { get; }

        private ISkuCatalog SkuCatalog { get; }

        private EnvironmentManagerSettings EnvironmentManagerSettings { get; }

        /// <inheritdoc/>
        public async Task<CloudEnvironmentServiceResult> ShutdownEnvironmentAsync(
            CloudEnvironment cloudEnvironment,
            IDiagnosticsLogger logger)
        {
            var duration = logger.StartDuration();

            try
            {
                Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
                Requires.NotNull(logger, nameof(logger));

                // Static Environment
                if (cloudEnvironment.Type == CloudEnvironmentType.StaticEnvironment)
                {
                    logger.AddDuration(duration)
                        .AddCloudEnvironment(cloudEnvironment)
                        .LogInfo(GetType().FormatLogMessage(nameof(ShutdownEnvironmentAsync)));

                    return new CloudEnvironmentServiceResult
                    {
                        CloudEnvironment = cloudEnvironment,
                        MessageCode = MessageCodes.ShutdownStaticEnvironment,
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
                    await ForceEnvironmentShutdownAsync(cloudEnvironment, logger);
                }
                else
                {
                    await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.ShuttingDown, CloudEnvironmentStateUpdateTriggers.ShutdownEnvironment, null, logger);

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
            CloudEnvironment cloudEnvironment,
            IDiagnosticsLogger logger)
        {
            var duration = logger.StartDuration();

            try
            {
                Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
                Requires.NotNull(logger, nameof(logger));

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
                    await ForceEnvironmentShutdownAsync(cloudEnvironment, logger);

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
            CloudEnvironment cloudEnvironment,
            StartCloudEnvironmentParameters startCloudEnvironmentParameters,
            IDiagnosticsLogger logger)
        {
            var duration = logger.StartDuration();

            try
            {
                Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
                Requires.NotNull(logger, nameof(logger));

                Requires.NotNull(startCloudEnvironmentParameters, nameof(startCloudEnvironmentParameters));

                // Static Environment
                if (cloudEnvironment.Type == CloudEnvironmentType.StaticEnvironment)
                {
                    logger.AddDuration(duration)
                        .AddCloudEnvironment(cloudEnvironment)
                        .LogInfo(GetType().FormatLogMessage(nameof(StartEnvironmentAsync)));

                    return new CloudEnvironmentServiceResult
                    {
                        CloudEnvironment = cloudEnvironment,
                        MessageCode = MessageCodes.StartStaticEnvironment,
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
                        MessageCode = MessageCodes.EnvironmentNotShutdown,
                        HttpStatusCode = StatusCodes.Status400BadRequest,
                    };
                }

                var connectionSessionId = cloudEnvironment.Connection?.ConnectionSessionId;
                if (!string.IsNullOrWhiteSpace(connectionSessionId))
                {
                    // Delete the previous liveshare session from database.
                    // Do not block start process on delete of old workspace from liveshare db.
                    _ = Task.Run(() => WorkspaceRepository.DeleteAsync(connectionSessionId, logger));
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
                        MessageCode = MessageCodes.UnableToAllocateResourcesWhileStarting,
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
                        MessageCode = MessageCodes.UnableToAllocateResourcesWhileStarting,
                        HttpStatusCode = StatusCodes.Status503ServiceUnavailable,
                    };
                }

                await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Starting, CloudEnvironmentStateUpdateTriggers.StartEnvironment, string.Empty, logger);
                await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, logger);

                // Kick off start-compute before returning.
                await StartComputeAsync(cloudEnvironment, null, startCloudEnvironmentParameters, logger);

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
                    await ShutdownEnvironmentAsync(cloudEnvironment, logger);
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
            StartCloudEnvironmentParameters startCloudEnvironmentParameters,
            VsoPlanInfo plan,
            IDiagnosticsLogger logger)
        {
            var duration = logger.StartDuration();

            var result = new CloudEnvironmentServiceResult()
            {
                MessageCode = MessageCodes.Unknown,
                HttpStatusCode = StatusCodes.Status409Conflict,
            };

            try
            {
                Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
                Requires.NotNull(cloudEnvironmentOptions, nameof(cloudEnvironmentOptions));
                Requires.NotNull(startCloudEnvironmentParameters, nameof(startCloudEnvironmentParameters));
                Requires.NotNull(plan, nameof(plan));
                Requires.NotNull(logger, nameof(logger));

                // Validate input
                ValidationUtil.IsRequired(cloudEnvironment.OwnerId, nameof(cloudEnvironment.OwnerId));

                ValidationUtil.IsRequired(cloudEnvironment.SkuName, nameof(cloudEnvironment.SkuName));
                ValidationUtil.IsTrue(
                    cloudEnvironment.Location != default,
                    "Location is required");
                ValidationUtil.IsRequired(cloudEnvironment.PlanId, nameof(CloudEnvironment.PlanId));
                ValidationUtil.IsRequired(cloudEnvironment.PlanId == plan.ResourceId);

                var environmentsInPlan = await ListEnvironmentsAsync(logger, planId: cloudEnvironment.PlanId);

                // Validate against existing environments.
                if (environmentsInPlan.Any(
                    (env) =>
                        /* TODO - when multiple users can access a plan, this should include an ownership check */
                        string.Equals(
                            env.FriendlyName,
                            cloudEnvironment.FriendlyName,
                            StringComparison.InvariantCultureIgnoreCase)))
                {
                    result.MessageCode = MessageCodes.EnvironmentNameAlreadyExists;
                    result.HttpStatusCode = StatusCodes.Status409Conflict;
                    return result;
                }

                var countOfEnvironmentsInPlan = environmentsInPlan.Count();
                var maxEnvironmentsForPlan = await EnvironmentManagerSettings.MaxEnvironmentsPerPlanAsync(plan.Subscription, logger);

                if (countOfEnvironmentsInPlan >= maxEnvironmentsForPlan)
                {
                    logger.AddDuration(duration)
                       .AddCloudEnvironment(cloudEnvironment)
                       .LogInfo(GetType().FormatLogErrorMessage(nameof(CreateEnvironmentAsync)));

                    result.MessageCode = MessageCodes.ExceededQuota;
                    result.HttpStatusCode = StatusCodes.Status403Forbidden;
                    return result;
                }

                // Setup
                cloudEnvironment.Id = Guid.NewGuid().ToString();
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
                    await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Created, CloudEnvironmentStateUpdateTriggers.CreateEnvironment, string.Empty, logger);
                    await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Provisioning, CloudEnvironmentStateUpdateTriggers.CreateEnvironment, string.Empty, logger);

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

                    result.MessageCode = MessageCodes.UnableToAllocateResources;
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
                    result.MessageCode = MessageCodes.UnableToAllocateResources;
                    result.HttpStatusCode = StatusCodes.Status503ServiceUnavailable;
                    return result;
                }

                // Create the cloud environment record in the provisioning state -- before starting.
                // This avoids a race condition where the record doesn't exist but the callback could be invoked.
                // Highly unlikely, but still...
                await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Provisioning, CloudEnvironmentStateUpdateTriggers.CreateEnvironment, string.Empty, logger);

                cloudEnvironment = await CloudEnvironmentRepository.CreateAsync(cloudEnvironment, logger);

                // Kick off start-compute before returning.
                await StartComputeAsync(cloudEnvironment, cloudEnvironmentOptions, startCloudEnvironmentParameters, logger);

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
                        await DeleteEnvironmentAsync(cloudEnvironment, logger);
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
            CloudEnvironment cloudEnvironment,
            EnvironmentRegistrationCallbackOptions options,
            IDiagnosticsLogger logger)
        {
            var duration = logger.StartDuration();

            try
            {
                Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
                Requires.NotNull(options, nameof(options));
                Requires.NotNull(logger, nameof(logger));

                ValidationUtil.IsTrue(cloudEnvironment.Connection.ConnectionSessionId == options.Payload.SessionId);

                cloudEnvironment.Connection.ConnectionSessionPath = options.Payload.SessionPath;
                await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Available, CloudEnvironmentStateUpdateTriggers.EnvironmentCallback, string.Empty, logger);
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
        public async Task<CloudEnvironment> GetEnvironmentWithStateRefreshAsync(
            string environmentId,
            IDiagnosticsLogger logger)
        {
            var duration = logger.StartDuration();
            var cloudEnvironment = default(CloudEnvironment);

            try
            {
                Requires.NotNullOrEmpty(environmentId, nameof(environmentId));
                Requires.NotNull(logger, nameof(logger));

                cloudEnvironment = await GetEnvironmentAsync(environmentId, logger);
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
                    // Remain in provisioning state until _callback is invoked.
                    case CloudEnvironmentState.Provisioning:

                        // Timeout if environment has stayed in provisioning state for more than an hour
                        var timeInProvisioningStateInMin = (DateTime.UtcNow - cloudEnvironment.LastStateUpdated).TotalMinutes;
                        if (timeInProvisioningStateInMin > 60)
                        {
                            newState = CloudEnvironmentState.Failed;
                            logger.AddCloudEnvironment(cloudEnvironment)
                                .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(GetEnvironmentWithStateRefreshAsync)), $"Marking environment creation failed with timeout. Time in provisioning state {timeInProvisioningStateInMin} minutes ");
                        }

                        break;

                    // Swap between available and awaiting based on the workspace status
                    case CloudEnvironmentState.Available:
                    case CloudEnvironmentState.Awaiting:
                        var sessionId = cloudEnvironment.Connection?.ConnectionSessionId;
                        var workspace = await WorkspaceRepository.GetStatusAsync(sessionId, logger);
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
                    await SetEnvironmentStateAsync(cloudEnvironment, newState, CloudEnvironmentStateUpdateTriggers.GetEnvironment, null, logger);
                    cloudEnvironment.Updated = DateTime.UtcNow;
                    cloudEnvironment = await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, logger);
                }

                return cloudEnvironment;
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .AddCloudEnvironment(cloudEnvironment)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(GetEnvironmentWithStateRefreshAsync)), ex.Message);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> ListEnvironmentsAsync(
            IDiagnosticsLogger logger,
            string planId = null,
            string environmentName = null,
            UserIdSet userIdSet = null)
        {
            Requires.NotNull(logger, nameof(logger));
            var environmentNameInLowerCase = environmentName?.Trim()?.ToLowerInvariant();

            /*
             * The code is written like this to optimize the CosmosDB lookups - consider that optimization if modifying it.
             */

            if (userIdSet == null)
            {
                Requires.NotNull(planId, nameof(planId));

                if (!string.IsNullOrEmpty(environmentNameInLowerCase))
                {
                    return await CloudEnvironmentRepository.GetWhereAsync(
                        (cloudEnvironment) => cloudEnvironment.PlanId == planId &&
                                              cloudEnvironment.FriendlyNameInLowerCase == environmentNameInLowerCase,
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
                if (!string.IsNullOrEmpty(environmentNameInLowerCase))
                {
                    Requires.NotNull(userIdSet, nameof(userIdSet));
                    return await CloudEnvironmentRepository.GetWhereAsync(
                        (cloudEnvironment) => (cloudEnvironment.OwnerId == userIdSet.CanonicalUserId ||
                                               cloudEnvironment.OwnerId == userIdSet.ProfileId) &&
                                              cloudEnvironment.FriendlyNameInLowerCase == environmentNameInLowerCase,
                        logger);
                }
                else
                {
                    return await CloudEnvironmentRepository.GetWhereAsync(
                        (cloudEnvironment) => cloudEnvironment.OwnerId == userIdSet.CanonicalUserId || cloudEnvironment.OwnerId == userIdSet.ProfileId, logger);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(environmentNameInLowerCase))
                {
                    Requires.NotNull(userIdSet, nameof(userIdSet));
                    return await CloudEnvironmentRepository.GetWhereAsync(
                        (cloudEnvironment) => (cloudEnvironment.OwnerId == userIdSet.CanonicalUserId ||
                                               cloudEnvironment.OwnerId == userIdSet.ProfileId) &&
                                              cloudEnvironment.PlanId == planId &&
                                              cloudEnvironment.FriendlyNameInLowerCase == environmentNameInLowerCase,
                        logger);
                }
                else
                {
                    return await CloudEnvironmentRepository.GetWhereAsync(
                        (cloudEnvironment) => (cloudEnvironment.OwnerId == userIdSet.CanonicalUserId ||
                                               cloudEnvironment.OwnerId == userIdSet.ProfileId) &&
                                              cloudEnvironment.PlanId == planId,
                        logger);
                }
            }
        }

        /// <inheritdoc/>
        public async Task<CloudEnvironment> GetEnvironmentAsync(
            string id,
            IDiagnosticsLogger logger)
        {
            ValidationUtil.IsRequired(id);
            Requires.NotNull(logger, nameof(logger));
            return await CloudEnvironmentRepository.GetAsync(id, logger);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteEnvironmentAsync(
            CloudEnvironment cloudEnvironment,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
            Requires.NotNull(logger, nameof(logger));

            /*
            // TODO: this delete logic isn't complete!
            // It should handle Workspace, Storage, and Compute, and CosmosDB independently.
            */

            await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Deleted, CloudEnvironmentStateUpdateTriggers.DeleteEnvironment, null, logger);

            if (cloudEnvironment.Type == CloudEnvironmentType.CloudEnvironment)
            {
                var storageIdToken = cloudEnvironment.Storage?.ResourceId;
                if (storageIdToken != null)
                {
                    await logger.OperationScopeAsync(
                        $"{GetType().GetLogMessageBaseName()}_delete_storage_async",
                        async (childLogger) =>
                        {
                            childLogger.FluentAddBaseValue(nameof(cloudEnvironment.Id), cloudEnvironment.Id)
                            .FluentAddBaseValue(nameof(storageIdToken), storageIdToken.Value);
                            await ResourceBrokerClient.DeleteResourceAsync(storageIdToken.Value, childLogger);
                        },
                        swallowException: true);
                }

                var computeIdToken = cloudEnvironment.Compute?.ResourceId;
                if (computeIdToken != null)
                {
                    await logger.OperationScopeAsync(
                       $"{GetType().GetLogMessageBaseName()}_delete_compute_async",
                       async (childLogger) =>
                       {
                           childLogger.FluentAddBaseValue(nameof(cloudEnvironment.Id), cloudEnvironment.Id)
                            .FluentAddBaseValue(nameof(computeIdToken), computeIdToken.Value);
                           await ResourceBrokerClient.DeleteResourceAsync(computeIdToken.Value, childLogger);
                       },
                       swallowException: true);
                }
            }

            if (cloudEnvironment.Connection?.ConnectionSessionId != null)
            {
                await logger.OperationScopeAsync(
                    $"{GetType().GetLogMessageBaseName()}_delete_workspace_async",
                    async (childLogger) =>
                    {
                        childLogger.FluentAddBaseValue(nameof(cloudEnvironment.Id), cloudEnvironment.Id)
                            .FluentAddBaseValue("ConnectionSessionId", cloudEnvironment.Connection?.ConnectionSessionId);
                        await WorkspaceRepository.DeleteAsync(cloudEnvironment.Connection.ConnectionSessionId, logger);
                    },
                    swallowException: true);
            }

            await CloudEnvironmentRepository.DeleteAsync(cloudEnvironment.Id, logger);

            return true;
        }

        /// <inheritdoc/>
        public async Task<CloudEnvironment> UpdateEnvironmentAsync(CloudEnvironment cloudEnvironment, CloudEnvironmentState newState, string trigger, string reason, IDiagnosticsLogger logger)
        {
            cloudEnvironment.Updated = DateTime.UtcNow;
            if (newState != default && newState != cloudEnvironment.State)
            {
                await SetEnvironmentStateAsync(cloudEnvironment, newState, trigger, reason, logger);
            }

            return await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, logger);
        }

        /// <inheritdoc/>
        public async Task ForceEnvironmentShutdownAsync(CloudEnvironment cloudEnvironment, IDiagnosticsLogger logger)
        {
            Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
            Requires.NotNull(logger, nameof(logger));

            await logger.OperationScopeAsync(
                $"{GetType().GetLogMessageBaseName()}_force_shutdown_async",
                async (childLogger) =>
                {
                    await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Shutdown, CloudEnvironmentStateUpdateTriggers.ForceEnvironmentShutdown, null, logger);
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

        /// <inheritdoc/>
        public async Task<CloudEnvironmentSettingsUpdateResult> UpdateEnvironmentSettingsAsync(
            CloudEnvironment cloudEnvironment,
            CloudEnvironmentUpdate update,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
            Requires.NotNull(update, nameof(update));
            Requires.NotNull(logger, nameof(logger));

            return await logger.OperationScopeAsync(
                $"{GetType().GetLogMessageBaseName()}_update_environment_settings_async",
                async (childLogger) =>
                {
                    childLogger
                        .AddCloudEnvironmentUpdate(update);

                    var allowedUpdates = GetEnvironmentAvailableSettingsUpdates(cloudEnvironment, logger);
                    var validationErrors = new List<MessageCodes>();

                    if (cloudEnvironment.State != CloudEnvironmentState.Shutdown)
                    {
                        validationErrors.Add(MessageCodes.EnvironmentNotShutdown);
                    }

                    if (update.AutoShutdownDelayMinutes.HasValue)
                    {
                        if (allowedUpdates.AllowedAutoShutdownDelayMinutes == null ||
                            !allowedUpdates.AllowedAutoShutdownDelayMinutes.Contains(update.AutoShutdownDelayMinutes.Value))
                        {
                            validationErrors.Add(MessageCodes.RequestedAutoShutdownDelayMinutesIsInvalid);
                        }
                        else
                        {
                            cloudEnvironment.AutoShutdownDelayMinutes = update.AutoShutdownDelayMinutes.Value;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(update.SkuName))
                    {
                        if (allowedUpdates.AllowedSkus == null || !allowedUpdates.AllowedSkus.Any())
                        {
                            validationErrors.Add(MessageCodes.UnableToUpdateSku);
                        }
                        else if (!allowedUpdates.AllowedSkus.Any((sku) => sku.SkuName == update.SkuName))
                        {
                            validationErrors.Add(MessageCodes.RequestedSkuIsInvalid);
                        }
                        else
                        {
                            // TODO - this assumes that the SKU change can be applied automatically on environment start.
                            // If the SKU change requires some other work then it should be applied here.
                            cloudEnvironment.SkuName = update.SkuName;
                        }
                    }

                    if (validationErrors.Any())
                    {
                        childLogger.AddErrorDetail($"Error MessageCodes: [ {string.Join(", ", validationErrors)} ]");

                        return CloudEnvironmentSettingsUpdateResult.Error(validationErrors);
                    }

                    cloudEnvironment.Updated = DateTime.UtcNow;
                    await SetEnvironmentStateAsync(cloudEnvironment, cloudEnvironment.State, CloudEnvironmentStateUpdateTriggers.EnvironmentSettingsChanged, null, logger);
                    cloudEnvironment = await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, logger);

                    return CloudEnvironmentSettingsUpdateResult.Success(cloudEnvironment);
                }, swallowException: false);
        }

        /// <inheritdoc/>
        public CloudEnvironmentAvailableSettingsUpdates GetEnvironmentAvailableSettingsUpdates(
            CloudEnvironment cloudEnvironment,
            IDiagnosticsLogger logger)
        {
            var duration = logger.StartDuration();

            try
            {
                Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
                Requires.NotNull(logger, nameof(logger));

                var result = new CloudEnvironmentAvailableSettingsUpdates();

                result.AllowedAutoShutdownDelayMinutes =
                    EnvironmentManagerSettings.DefaultAutoShutdownDelayMinutesOptions?.ToArray() ??
                    Array.Empty<int>();

                if (
                    SkuCatalog.CloudEnvironmentSkus.TryGetValue(cloudEnvironment.SkuName, out var currentSku) &&
                    currentSku.SupportedSkuTransitions != null &&
                    currentSku.SupportedSkuTransitions.Any())
                {
                    result.AllowedSkus = currentSku.SupportedSkuTransitions
                        .Select((skuName) =>
                        {
                            SkuCatalog.CloudEnvironmentSkus.TryGetValue(skuName, out var sku);
                            return sku;
                        })
                        .Where((sku) =>
                            sku != null &&
                            sku.SkuLocations.Contains(cloudEnvironment.Location))
                        .ToArray();
                }
                else
                {
                    result.AllowedSkus = Array.Empty<ICloudEnvironmentSku>();
                }

                logger.AddDuration(duration)
                    .AddCloudEnvironment(cloudEnvironment)
                    .LogInfo(GetType().FormatLogMessage(nameof(GetEnvironmentAvailableSettingsUpdates)));

                return result;
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .AddCloudEnvironment(cloudEnvironment)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(GetEnvironmentAvailableSettingsUpdates)), ex.Message);
                throw;
            }
        }

        private async Task SetEnvironmentStateAsync(
            CloudEnvironment cloudEnvironment,
            CloudEnvironmentState state,
            string trigger,
            string reason,
            IDiagnosticsLogger logger)
        {
            var oldState = cloudEnvironment.State;
            var oldStateUpdated = cloudEnvironment.LastStateUpdated;
            logger.FluentAddBaseValue("OldState", oldState)
                .FluentAddBaseValue("OldStateUpdated", oldStateUpdated);

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
                plan, environment, BillingEventTypes.EnvironmentStateChange, stateChange, logger.NewChildLogger());

            cloudEnvironment.State = state;
            cloudEnvironment.LastStateUpdateTrigger = trigger;
            cloudEnvironment.LastStateUpdated = DateTime.UtcNow;

            if (reason != null)
            {
                cloudEnvironment.LastStateUpdateReason = reason;
            }

            logger.AddCloudEnvironment(cloudEnvironment)
                 .LogInfo(GetType().FormatLogMessage(nameof(SetEnvironmentStateAsync)));
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

            var workspaceResponse = await WorkspaceRepository.CreateAsync(workspaceRequest, logger);
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

        private async Task StartComputeAsync(
            CloudEnvironment cloudEnvironment,
            CloudEnvironmentOptions cloudEnvironmentOptions,
            StartCloudEnvironmentParameters startCloudEnvironmentParameters,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
            Requires.NotNull(cloudEnvironment.Compute, nameof(cloudEnvironment.Compute));
            Requires.NotNull(cloudEnvironment.Storage, nameof(cloudEnvironment.Storage));
            Requires.NotEmpty(cloudEnvironment.Compute.ResourceId, $"{nameof(cloudEnvironment.Compute)}.{nameof(cloudEnvironment.Compute.ResourceId)}");
            Requires.NotEmpty(cloudEnvironment.Storage.ResourceId, $"{nameof(cloudEnvironment.Storage)}.{nameof(cloudEnvironment.Storage.ResourceId)}");

            Requires.NotNull(startCloudEnvironmentParameters, nameof(startCloudEnvironmentParameters));
            Requires.NotNullOrEmpty(startCloudEnvironmentParameters.CallbackUriFormat, nameof(startCloudEnvironmentParameters.CallbackUriFormat));
            Requires.NotNull(startCloudEnvironmentParameters.ServiceUri, nameof(startCloudEnvironmentParameters.ServiceUri));
            UnauthorizedUtil.IsRequired(startCloudEnvironmentParameters.AccessToken);

            var callbackUri = new Uri(string.Format(startCloudEnvironmentParameters.CallbackUriFormat, cloudEnvironment.Id));
            Requires.Argument(callbackUri.IsAbsoluteUri, nameof(callbackUri), "Must be an absolute URI.");

            var cascadeToken = await AuthRepository.ExchangeToken(startCloudEnvironmentParameters.AccessToken);
            UnauthorizedUtil.IsRequired(cascadeToken);

            // Construct the start-compute environment variables
            var environmentVariables = EnvironmentVariableGenerator.Generate(
                cloudEnvironment,
                startCloudEnvironmentParameters.ServiceUri,
                callbackUri,
                startCloudEnvironmentParameters.AccessToken,
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
