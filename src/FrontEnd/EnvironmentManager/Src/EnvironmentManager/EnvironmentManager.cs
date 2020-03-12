// <copyright file="EnvironmentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repositories;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <inheritdoc/>
    public class EnvironmentManager : IEnvironmentManager
    {
        private const string LogBaseName = "environment_manager";
        private const int PersistentSessionExpiresInDays = 30;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentManager"/> class.
        /// </summary>
        /// <param name="cloudEnvironmentRepository">The cloud environment repository.</param>
        /// <param name="resourceBrokerHttpClient">The resource broker client.</param>
        /// <param name="workspaceRepository">The Live Share workspace repository.</param>
        /// <param name="authRepository">The Live Share authentication repository.</param>
        /// <param name="tokenProvider">Provider capable of issuing access tokens.</param>
        /// <param name="billingEventManager">The billing event manager.</param>
        /// <param name="skuCatalog">The SKU catalog.</param>
        /// <param name="environmentMonitor">The environment monitor.</param>
        /// <param name="environmentManagerSettings">The environment manager settings.</param>
        /// <param name="planManagerSettings">The plan manager settings.</param>
        public EnvironmentManager(
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IResourceBrokerResourcesHttpContract resourceBrokerHttpClient,
            IWorkspaceRepository workspaceRepository,
            ITokenProvider tokenProvider,
            IBillingEventManager billingEventManager,
            ISkuCatalog skuCatalog,
            IEnvironmentMonitor environmentMonitor,
            EnvironmentManagerSettings environmentManagerSettings,
            PlanManagerSettings planManagerSettings)
        {
            CloudEnvironmentRepository = Requires.NotNull(cloudEnvironmentRepository, nameof(cloudEnvironmentRepository));
            WorkspaceRepository = Requires.NotNull(workspaceRepository, nameof(workspaceRepository));
            ResourceBrokerClient = Requires.NotNull(resourceBrokerHttpClient, nameof(resourceBrokerHttpClient));
            TokenProvider = Requires.NotNull(tokenProvider, nameof(tokenProvider));
            BillingEventManager = Requires.NotNull(billingEventManager, nameof(billingEventManager));
            SkuCatalog = skuCatalog;
            EnvironmentMonitor = environmentMonitor;
            EnvironmentManagerSettings = Requires.NotNull(environmentManagerSettings, nameof(environmentManagerSettings));
            PlanManagerSettings = Requires.NotNull(planManagerSettings, nameof(PlanManagerSettings));
        }

        private ICloudEnvironmentRepository CloudEnvironmentRepository { get; }

        private IWorkspaceRepository WorkspaceRepository { get; }

        private ITokenProvider TokenProvider { get; }

        private IResourceBrokerResourcesHttpContract ResourceBrokerClient { get; }

        private IBillingEventManager BillingEventManager { get; }

        private ISkuCatalog SkuCatalog { get; }

        private IEnvironmentMonitor EnvironmentMonitor { get; }

        private EnvironmentManagerSettings EnvironmentManagerSettings { get; }

        private PlanManagerSettings PlanManagerSettings { get; }

        /// <inheritdoc/>
        public Task<CloudEnvironment> GetAsync(
            string id,
            IDiagnosticsLogger logger)
        {
            ValidationUtil.IsRequired(id);
            Requires.NotNull(logger, nameof(logger));

            return logger.OperationScopeAsync(
                $"{LogBaseName}_get",
                async (childLogger) =>
                {
                    return await CloudEnvironmentRepository.GetAsync(id, childLogger.NewChildLogger());
                });
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> GetAndStateRefreshAsync(
            string environmentId,
            IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(environmentId, nameof(environmentId));
            Requires.NotNull(logger, nameof(logger));

            return logger.OperationScopeAsync(
                $"{LogBaseName}_get_environment_state_refresh",
                async (childLogger) =>
                {
                    var cloudEnvironment = await GetAsync(environmentId, childLogger.NewChildLogger());
                    if (cloudEnvironment is null)
                    {
                        return null;
                    }

                    // Update the new state before returning.
                    var originalState = cloudEnvironment.State;
                    var newState = originalState;

                    // TODO: Remove once Anu's update tracking is in.
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

                                childLogger.LogErrorWithDetail($"{LogBaseName}_get_environment_state_refresh_error", $"Marking environment creation failed with timeout. Time in provisioning state {timeInProvisioningStateInMin} minutes.");
                            }

                            break;

                        // Swap between available and awaiting based on the workspace status
                        case CloudEnvironmentState.Available:
                        case CloudEnvironmentState.Awaiting:
                            var sessionId = cloudEnvironment.Connection?.ConnectionSessionId;
                            var workspace = await WorkspaceRepository.GetStatusAsync(sessionId, childLogger.NewChildLogger());
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
                        await SetEnvironmentStateAsync(cloudEnvironment, newState, CloudEnvironmentStateUpdateTriggers.GetEnvironment, null, childLogger.NewChildLogger());

                        cloudEnvironment.Updated = DateTime.UtcNow;

                        cloudEnvironment = await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, childLogger.NewChildLogger());
                    }

                    return cloudEnvironment;
                });
        }

        /// <inheritdoc/>
        public Task<IEnumerable<CloudEnvironment>> ListAsync(
            IDiagnosticsLogger logger,
            string planId = null,
            string environmentName = null,
            UserIdSet userIdSet = null)
        {
            Requires.NotNull(logger, nameof(logger));

            return logger.OperationScopeAsync(
                $"{LogBaseName}_list",
                async (childLogger) =>
                {
                    var environmentNameInLowerCase = environmentName?.Trim()?.ToLowerInvariant();

                    // The code is written like this to optimize the CosmosDB lookups - consider that optimization if modifying it.
                    if (userIdSet == null)
                    {
                        Requires.NotNull(planId, nameof(planId));

                        if (!string.IsNullOrEmpty(environmentNameInLowerCase))
                        {
                            return await CloudEnvironmentRepository.GetWhereAsync(
                                (cloudEnvironment) => cloudEnvironment.PlanId == planId &&
                                    cloudEnvironment.FriendlyNameInLowerCase == environmentNameInLowerCase,
                                childLogger.NewChildLogger());
                        }
                        else
                        {
                            return await CloudEnvironmentRepository.GetWhereAsync(
                                (cloudEnvironment) => cloudEnvironment.PlanId == planId,
                                childLogger.NewChildLogger());
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
                                childLogger.NewChildLogger());
                        }
                        else
                        {
                            return await CloudEnvironmentRepository.GetWhereAsync(
                                (cloudEnvironment) => cloudEnvironment.OwnerId == userIdSet.CanonicalUserId ||
                                    cloudEnvironment.OwnerId == userIdSet.ProfileId,
                                childLogger.NewChildLogger());
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
                                childLogger.NewChildLogger());
                        }
                        else
                        {
                            return await CloudEnvironmentRepository.GetWhereAsync(
                                (cloudEnvironment) => (cloudEnvironment.OwnerId == userIdSet.CanonicalUserId ||
                                        cloudEnvironment.OwnerId == userIdSet.ProfileId) &&
                                    cloudEnvironment.PlanId == planId,
                                childLogger.NewChildLogger());
                        }
                    }
                });
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> UpdateAsync(CloudEnvironment cloudEnvironment, CloudEnvironmentState newState, string trigger, string reason, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_update",
                async (childLogger) =>
                {
                    childLogger.AddCloudEnvironment(cloudEnvironment);

                    cloudEnvironment.Updated = DateTime.UtcNow;
                    if (newState != default && newState != cloudEnvironment.State)
                    {
                        await SetEnvironmentStateAsync(cloudEnvironment, newState, trigger, reason, childLogger.NewChildLogger());
                    }

                    return await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, childLogger.NewChildLogger());
                });
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> UpdateCallbackAsync(
            CloudEnvironment cloudEnvironment,
            EnvironmentRegistrationCallbackOptions options,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
            Requires.NotNull(options, nameof(options));
            Requires.NotNull(logger, nameof(logger));

            ValidationUtil.IsTrue(cloudEnvironment.Connection.ConnectionSessionId == options.Payload.SessionId);

            return logger.OperationScopeAsync(
                $"{LogBaseName}_update_callback",
                async (childLogger) =>
                {
                    childLogger.AddCloudEnvironment(cloudEnvironment);

                    cloudEnvironment.Connection.ConnectionSessionPath = options.Payload.SessionPath;

                    await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Available, CloudEnvironmentStateUpdateTriggers.EnvironmentCallback, string.Empty, childLogger.NewChildLogger());

                    cloudEnvironment.Updated = DateTime.UtcNow;

                    return await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, childLogger.NewChildLogger());
                });
        }

        /// <inheritdoc/>
        public Task<CloudEnvironmentServiceResult> CreateAsync(
            CloudEnvironment cloudEnvironment,
            CloudEnvironmentOptions cloudEnvironmentOptions,
            StartCloudEnvironmentParameters startCloudEnvironmentParameters,
            VsoPlanInfo plan,
            IDiagnosticsLogger logger)
        {
            // Validate input
            Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
            Requires.NotNull(cloudEnvironmentOptions, nameof(cloudEnvironmentOptions));
            Requires.NotNull(startCloudEnvironmentParameters, nameof(startCloudEnvironmentParameters));
            Requires.NotNull(plan, nameof(plan));
            Requires.NotNull(logger, nameof(logger));

            ValidationUtil.IsRequired(cloudEnvironment.OwnerId, nameof(cloudEnvironment.OwnerId));
            ValidationUtil.IsRequired(cloudEnvironment.SkuName, nameof(cloudEnvironment.SkuName));
            ValidationUtil.IsTrue(cloudEnvironment.Location != default, "Location is required");
            ValidationUtil.IsRequired(cloudEnvironment.PlanId, nameof(CloudEnvironment.PlanId));
            ValidationUtil.IsRequired(cloudEnvironment.PlanId == plan.ResourceId);

            return logger.OperationScopeAsync(
                $"{LogBaseName}_create",
                async (childLogger) =>
                {
                    childLogger.AddCloudEnvironment(cloudEnvironment);

                    var result = new CloudEnvironmentServiceResult()
                    {
                        MessageCode = MessageCodes.Unknown,
                        HttpStatusCode = StatusCodes.Status409Conflict,
                    };

                    var environmentsInPlan = await ListAsync(childLogger.NewChildLogger(), planId: cloudEnvironment.PlanId);

                    // Validate against existing environments.
                    // TODO - when multiple users can access a plan, this should include an ownership check
                    if (environmentsInPlan.Any(
                        (env) => string.Equals(env.FriendlyName, cloudEnvironment.FriendlyName, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        result.MessageCode = MessageCodes.EnvironmentNameAlreadyExists;
                        result.HttpStatusCode = StatusCodes.Status409Conflict;

                        return result;
                    }

                    var countOfEnvironmentsInPlan = environmentsInPlan.Count();
                    var maxEnvironmentsForPlan = await EnvironmentManagerSettings.MaxEnvironmentsPerPlanAsync(plan.Subscription, childLogger.NewChildLogger());

                    if (countOfEnvironmentsInPlan >= maxEnvironmentsForPlan)
                    {
                        childLogger.LogError($"{LogBaseName}_create_maxenvironmentsforplan_error");

                        result.MessageCode = MessageCodes.ExceededQuota;
                        result.HttpStatusCode = StatusCodes.Status403Forbidden;

                        return result;
                    }

                    // Setup
                    cloudEnvironment.Id = Guid.NewGuid().ToString();
                    cloudEnvironment.Created = cloudEnvironment.Updated = cloudEnvironment.LastUsed = DateTime.UtcNow;

                    // Static Environment
                    if (cloudEnvironment.Type == EnvironmentType.StaticEnvironment)
                    {
                        if (cloudEnvironment.Connection is null)
                        {
                            cloudEnvironment.Connection = new ConnectionInfo();
                        }

                        if (string.IsNullOrWhiteSpace(cloudEnvironment.Connection.ConnectionSessionId))
                        {
                            cloudEnvironment.Connection = await CreateWorkspace(
                                EnvironmentType.StaticEnvironment,
                                cloudEnvironment.Id,
                                Guid.Empty,
                                startCloudEnvironmentParameters.ConnectionServiceUri,
                                childLogger.NewChildLogger());
                        }

                        if (cloudEnvironment.Seed == default || cloudEnvironment.Seed.SeedType != SeedType.StaticEnvironment)
                        {
                            cloudEnvironment.Seed = new SeedInfo { SeedType = SeedType.StaticEnvironment };
                        }

                        cloudEnvironment.SkuName = StaticEnvironmentSku.Name;

                        // Environments must be initialized in Created state. But (at least for now) new environments immediately transition to Provisioning state.
                        await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Created, CloudEnvironmentStateUpdateTriggers.CreateEnvironment, string.Empty, childLogger.NewChildLogger());
                        await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Provisioning, CloudEnvironmentStateUpdateTriggers.CreateEnvironment, string.Empty, childLogger.NewChildLogger());

                        cloudEnvironment = await CloudEnvironmentRepository.CreateAsync(cloudEnvironment, childLogger.NewChildLogger());

                        result.CloudEnvironment = cloudEnvironment;
                        result.HttpStatusCode = StatusCodes.Status200OK;

                        return result;
                    }

                    // Allocate Storage and Compute
                    try
                    {
                        var allocationResult = await AllocateComputeAndStorageAsync(cloudEnvironment, childLogger.NewChildLogger());
                        cloudEnvironment.Storage = allocationResult.Storage;
                        cloudEnvironment.Compute = allocationResult.Compute;
                    }
                    catch (Exception ex) when (ex is RemoteInvocationException || ex is HttpResponseStatusException)
                    {
                        childLogger.LogError($"{LogBaseName}_create_allocate_error");

                        result.MessageCode = MessageCodes.UnableToAllocateResources;
                        result.HttpStatusCode = StatusCodes.Status503ServiceUnavailable;

                        return result;
                    }

                    // Start Environment Monitoring
                    try
                    {
                        await EnvironmentMonitor.MonitorHeartbeatAsync(cloudEnvironment.Id, cloudEnvironment.Compute.ResourceId, logger.NewChildLogger());
                    }
                    catch (Exception ex)
                    {
                        childLogger.LogException($"{LogBaseName}_create_monitor_error", ex);
                        result.MessageCode = MessageCodes.UnableToAllocateResources;
                        result.HttpStatusCode = StatusCodes.Status503ServiceUnavailable;

                        // Delete the allocated resources.
                        if (cloudEnvironment.Compute != null)
                        {
                            await ResourceBrokerClient.DeleteAsync(cloudEnvironment.Compute.ResourceId, childLogger.NewChildLogger());
                        }

                        if (cloudEnvironment.Storage != null)
                        {
                            await ResourceBrokerClient.DeleteAsync(cloudEnvironment.Storage.ResourceId, childLogger.NewChildLogger());
                        }

                        return result;
                    }

                    // Create the Live Share workspace
                    cloudEnvironment.Connection = await CreateWorkspace(
                        EnvironmentType.CloudEnvironment,
                        cloudEnvironment.Id,
                        cloudEnvironment.Compute.ResourceId,
                        startCloudEnvironmentParameters.ConnectionServiceUri,
                        childLogger.NewChildLogger());
                    if (string.IsNullOrWhiteSpace(cloudEnvironment.Connection.ConnectionSessionId))
                    {
                        childLogger.LogErrorWithDetail($"{LogBaseName}_create_workspace_error", "Could not create the cloud environment workspace session.");

                        result.MessageCode = MessageCodes.UnableToAllocateResources;
                        result.HttpStatusCode = StatusCodes.Status503ServiceUnavailable;

                        return result;
                    }

                    // Create the cloud environment record in the provisioning state -- before starting.
                    // This avoids a race condition where the record doesn't exist but the callback could be invoked.
                    // Highly unlikely, but still...
                    await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Provisioning, CloudEnvironmentStateUpdateTriggers.CreateEnvironment, string.Empty, childLogger.NewChildLogger());

                    cloudEnvironment = await CloudEnvironmentRepository.CreateAsync(cloudEnvironment, childLogger.NewChildLogger());

                    // Kick off start-compute before returning.
                    await StartComputeAsync(cloudEnvironment, cloudEnvironmentOptions, startCloudEnvironmentParameters, childLogger.NewChildLogger());

                    result.CloudEnvironment = cloudEnvironment;
                    result.HttpStatusCode = StatusCodes.Status200OK;

                    return result;
                },
                async (ex, childLogger) =>
                {
                    if (cloudEnvironment.Id != null)
                    {
                        // TODO: This won't actually cleanup properly because it relies on the workspace,
                        //       compute, and storage to have been written to the database!
                        // Compensating cleanup
                        await DeleteAsync(cloudEnvironment, childLogger.NewChildLogger());
                    }

                    return default(CloudEnvironmentServiceResult);
                });
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(
            CloudEnvironment cloudEnvironment,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
            Requires.NotNull(logger, nameof(logger));

            return logger.OperationScopeAsync(
                $"{LogBaseName}_delete",
                async (childLogger) =>
                {
                    await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Deleted, CloudEnvironmentStateUpdateTriggers.DeleteEnvironment, null, childLogger.NewChildLogger());

                    if (cloudEnvironment.Type == EnvironmentType.CloudEnvironment)
                    {
                        var storageIdToken = cloudEnvironment.Storage?.ResourceId;
                        if (storageIdToken != null)
                        {
                            await childLogger.OperationScopeAsync(
                                $"{LogBaseName}_delete_storage",
                                async (innerLogger) =>
                                {
                                    innerLogger.FluentAddBaseValue(nameof(cloudEnvironment.Id), cloudEnvironment.Id)
                                        .FluentAddBaseValue(nameof(storageIdToken), storageIdToken.Value);

                                    await ResourceBrokerClient.DeleteAsync(storageIdToken.Value, innerLogger.NewChildLogger());
                                },
                                swallowException: true);
                        }

                        var computeIdToken = cloudEnvironment.Compute?.ResourceId;
                        if (computeIdToken != null)
                        {
                            await childLogger.OperationScopeAsync(
                               $"{LogBaseName}_delete_compute",
                               async (innerLogger) =>
                               {
                                   innerLogger.FluentAddBaseValue(nameof(cloudEnvironment.Id), cloudEnvironment.Id)
                                        .FluentAddBaseValue(nameof(computeIdToken), computeIdToken.Value);

                                   await ResourceBrokerClient.DeleteAsync(computeIdToken.Value, innerLogger.NewChildLogger());
                               },
                               swallowException: true);
                        }
                    }

                    if (cloudEnvironment.Connection?.ConnectionSessionId != null)
                    {
                        await childLogger.OperationScopeAsync(
                            $"{LogBaseName}_delete_workspace",
                            async (innerLogger) =>
                            {
                                innerLogger.FluentAddBaseValue(nameof(cloudEnvironment.Id), cloudEnvironment.Id)
                                    .FluentAddBaseValue("ConnectionSessionId", cloudEnvironment.Connection?.ConnectionSessionId);

                                await WorkspaceRepository.DeleteAsync(cloudEnvironment.Connection.ConnectionSessionId, innerLogger.NewChildLogger());
                            },
                            swallowException: true);
                    }

                    await CloudEnvironmentRepository.DeleteAsync(cloudEnvironment.Id, childLogger.NewChildLogger());

                    return true;
                });
        }

        /// <inheritdoc/>
        public Task<CloudEnvironmentServiceResult> ResumeAsync(
            CloudEnvironment cloudEnvironment,
            StartCloudEnvironmentParameters startCloudEnvironmentParameters,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
            Requires.NotNull(logger, nameof(logger));
            Requires.NotNull(startCloudEnvironmentParameters, nameof(startCloudEnvironmentParameters));

            return logger.OperationScopeAsync(
                $"{LogBaseName}_resume",
                async (childLogger) =>
                {
                    childLogger.AddCloudEnvironment(cloudEnvironment);

                    // Static Environment
                    if (cloudEnvironment.Type == EnvironmentType.StaticEnvironment)
                    {
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
                        _ = Task.Run(() => WorkspaceRepository.DeleteAsync(connectionSessionId, childLogger.NewChildLogger()));
                        cloudEnvironment.Connection = null;
                    }

                    // Allocate Compute
                    try
                    {
                        cloudEnvironment.Compute = await AllocateComputeAsync(cloudEnvironment, childLogger.NewChildLogger());
                    }
                    catch (Exception ex) when (ex is RemoteInvocationException || ex is HttpResponseStatusException)
                    {
                        childLogger.LogException($"{LogBaseName}_resume_allocate_error", ex);

                        return new CloudEnvironmentServiceResult
                        {
                            MessageCode = MessageCodes.UnableToAllocateResourcesWhileStarting,
                            HttpStatusCode = StatusCodes.Status503ServiceUnavailable,
                        };
                    }

                    // Start Environment Monitoring
                    try
                    {
                        await EnvironmentMonitor.MonitorHeartbeatAsync(cloudEnvironment.Id, cloudEnvironment.Compute.ResourceId, childLogger.NewChildLogger());
                    }
                    catch (Exception ex)
                    {
                        childLogger.LogException($"{LogBaseName}_create_monitor_error", ex);

                        // Delete the allocated resources.
                        if (cloudEnvironment.Compute != null)
                        {
                            await ResourceBrokerClient.DeleteAsync(cloudEnvironment.Compute.ResourceId, childLogger.NewChildLogger());
                        }

                        return new CloudEnvironmentServiceResult
                        {
                            MessageCode = MessageCodes.UnableToAllocateResourcesWhileStarting,
                            HttpStatusCode = StatusCodes.Status503ServiceUnavailable,
                        };
                    }

                    // Create the Live Share workspace
                    cloudEnvironment.Connection = await CreateWorkspace(
                        EnvironmentType.CloudEnvironment,
                        cloudEnvironment.Id,
                        cloudEnvironment.Compute.ResourceId,
                        startCloudEnvironmentParameters.ConnectionServiceUri,
                        childLogger.NewChildLogger());
                    if (string.IsNullOrWhiteSpace(cloudEnvironment.Connection.ConnectionSessionId))
                    {
                        childLogger.LogErrorWithDetail($"{LogBaseName}_resume_workspace_error", "Could not create the cloud environment workspace session.");

                        return new CloudEnvironmentServiceResult
                        {
                            MessageCode = MessageCodes.UnableToAllocateResourcesWhileStarting,
                            HttpStatusCode = StatusCodes.Status503ServiceUnavailable,
                        };
                    }

                    await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Starting, CloudEnvironmentStateUpdateTriggers.StartEnvironment, string.Empty, childLogger.NewChildLogger());

                    await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, childLogger.NewChildLogger());

                    // Kick off start-compute before returning.
                    await StartComputeAsync(cloudEnvironment, null, startCloudEnvironmentParameters, childLogger.NewChildLogger());

                    // Kick off state transition monitoring.
                    try
                    {
                        await EnvironmentMonitor.MonitorResumeStateTransitionAsync(cloudEnvironment.Id, cloudEnvironment.Compute.ResourceId, childLogger.NewChildLogger());
                    }
                    catch (Exception ex)
                    {
                        childLogger.LogException($"{LogBaseName}_create_state_transition_monitor_error", ex);

                        // Delete the allocated resources.
                        if (cloudEnvironment.Compute != null)
                        {
                            await ResourceBrokerClient.DeleteAsync(cloudEnvironment.Compute.ResourceId, childLogger.NewChildLogger());
                        }

                        return new CloudEnvironmentServiceResult
                        {
                            MessageCode = MessageCodes.UnableToAllocateResourcesWhileStarting,
                            HttpStatusCode = StatusCodes.Status503ServiceUnavailable,
                        };
                    }

                    return new CloudEnvironmentServiceResult
                    {
                        CloudEnvironment = cloudEnvironment,
                        HttpStatusCode = StatusCodes.Status200OK,
                    };
                },
                async (e, childLogger) =>
                {
                    await SuspendAsync(cloudEnvironment, childLogger.NewChildLogger());

                    return default(CloudEnvironmentServiceResult);
                });
        }

        /// <inheritdoc/>
        public Task<CloudEnvironmentServiceResult> SuspendAsync(
            CloudEnvironment cloudEnvironment,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
            Requires.NotNull(logger, nameof(logger));

            return logger.OperationScopeAsync(
                $"{LogBaseName}_suspsend",
                async (childLogger) =>
                {
                    childLogger.AddCloudEnvironment(cloudEnvironment);

                    // Static Environment
                    if (cloudEnvironment.Type == EnvironmentType.StaticEnvironment)
                    {
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
                        return await ForceSuspendAsync(cloudEnvironment, childLogger.NewChildLogger());
                    }
                    else
                    {
                        await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.ShuttingDown, CloudEnvironmentStateUpdateTriggers.ShutdownEnvironment, null, childLogger.NewChildLogger());

                        // Update the database state.
                        cloudEnvironment = await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, childLogger.NewChildLogger());

                        // Start the cleanup operation to shutdown environment.
                        await ResourceBrokerClient.SuspendAsync(
                            Guid.Parse(cloudEnvironment.Id), new List<SuspendRequestBody> { new SuspendRequestBody { ResourceId = cloudEnvironment.Compute.ResourceId } }, childLogger.NewChildLogger());

                        return new CloudEnvironmentServiceResult
                        {
                            CloudEnvironment = cloudEnvironment,
                            HttpStatusCode = StatusCodes.Status200OK,
                        };
                    }
                });
        }

        /// <inheritdoc/>
        public Task<CloudEnvironmentServiceResult> SuspendCallbackAsync(
            CloudEnvironment cloudEnvironment,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
            Requires.NotNull(logger, nameof(logger));

            return logger.OperationScopeAsync(
                $"{LogBaseName}_suspsend_callback",
                async (childLogger) =>
                {
                    childLogger.AddCloudEnvironment(cloudEnvironment);

                    // Static Environment
                    if (cloudEnvironment.Type == EnvironmentType.StaticEnvironment)
                    {
                        return new CloudEnvironmentServiceResult
                        {
                            CloudEnvironment = cloudEnvironment,
                            MessageCode = MessageCodes.ShutdownStaticEnvironment,
                            HttpStatusCode = StatusCodes.Status400BadRequest,
                        };
                    }

                    if (cloudEnvironment.State == CloudEnvironmentState.ShuttingDown)
                    {
                        return await ForceSuspendAsync(cloudEnvironment, childLogger.NewChildLogger());
                    }

                    return new CloudEnvironmentServiceResult
                    {
                        CloudEnvironment = cloudEnvironment,
                        HttpStatusCode = StatusCodes.Status200OK,
                    };
                });
        }

        /// <inheritdoc/>
        public async Task<CloudEnvironmentServiceResult> ForceSuspendAsync(CloudEnvironment cloudEnvironment, IDiagnosticsLogger logger)
        {
            Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
            Requires.NotNull(logger, nameof(logger));

            return await logger.OperationScopeAsync(
                $"{LogBaseName}_force_suspend",
                async (childLogger) =>
                {
                    await SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Shutdown, CloudEnvironmentStateUpdateTriggers.ForceEnvironmentShutdown, null, logger);

                    var computeIdToken = cloudEnvironment.Compute?.ResourceId;
                    cloudEnvironment.Compute = null;

                    // Update the database state.
                    cloudEnvironment = await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, childLogger.NewChildLogger());

                    // Delete the allocated resources.
                    if (computeIdToken != null)
                    {
                        await ResourceBrokerClient.DeleteAsync(computeIdToken.Value, childLogger.NewChildLogger());
                    }

                    return new CloudEnvironmentServiceResult
                    {
                        CloudEnvironment = cloudEnvironment,
                        HttpStatusCode = StatusCodes.Status200OK,
                    };
                },
                swallowException: true);
        }

        /// <inheritdoc/>
        public async Task<CloudEnvironmentSettingsUpdateResult> UpdateSettingsAsync(
            CloudEnvironment cloudEnvironment,
            CloudEnvironmentUpdate update,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
            Requires.NotNull(update, nameof(update));
            Requires.NotNull(logger, nameof(logger));

            return await logger.OperationScopeAsync(
                $"{LogBaseName}_update_settings",
                async (childLogger) =>
                {
                    childLogger.AddCloudEnvironmentUpdate(update);

                    var allowedUpdates = await GetAvailableSettingsUpdatesAsync(cloudEnvironment, childLogger.NewChildLogger());
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

                    await SetEnvironmentStateAsync(
                        cloudEnvironment, cloudEnvironment.State, CloudEnvironmentStateUpdateTriggers.EnvironmentSettingsChanged, null, childLogger.NewChildLogger());

                    cloudEnvironment = await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, childLogger.NewChildLogger());

                    return CloudEnvironmentSettingsUpdateResult.Success(cloudEnvironment);
                },
                swallowException: false);
        }

        /// <inheritdoc/>
        public Task<CloudEnvironmentAvailableSettingsUpdates> GetAvailableSettingsUpdatesAsync(
            CloudEnvironment cloudEnvironment,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
            Requires.NotNull(logger, nameof(logger));

            return logger.OperationScopeAsync(
                $"{LogBaseName}_get_available_settings_updates",
                (childLogger) =>
                {
                    childLogger.AddCloudEnvironment(cloudEnvironment);

                    var result = new CloudEnvironmentAvailableSettingsUpdates();

                    result.AllowedAutoShutdownDelayMinutes = PlanManagerSettings.DefaultAutoSuspendDelayMinutesOptions;

                    if (SkuCatalog.CloudEnvironmentSkus.TryGetValue(cloudEnvironment.SkuName, out var currentSku) &&
                        currentSku.SupportedSkuTransitions != null &&
                        currentSku.SupportedSkuTransitions.Any())
                    {
                        result.AllowedSkus = currentSku.SupportedSkuTransitions
                            .Select((skuName) =>
                            {
                                SkuCatalog.CloudEnvironmentSkus.TryGetValue(skuName, out var sku);
                                return sku;
                            })
                            .Where((sku) => sku != null && sku.SkuLocations.Contains(cloudEnvironment.Location))
                            .ToArray();
                    }
                    else
                    {
                        result.AllowedSkus = Array.Empty<ICloudEnvironmentSku>();
                    }

                    return Task.FromResult(result);
                });
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
            EnvironmentType type,
            string cloudEnvironmentId,
            Guid computeIdToken,
            Uri connectionServiceUri,
            IDiagnosticsLogger logger)
        {
            var duration = logger.StartDuration();

            Requires.NotNullOrEmpty(cloudEnvironmentId, nameof(cloudEnvironmentId));
            Requires.NotNull(connectionServiceUri, nameof(connectionServiceUri));

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
                ConnectionServiceUri = connectionServiceUri.AbsoluteUri,
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
            var computeRequest = new AllocateRequestBody
            {
                Type = ResourceType.ComputeVM,
                SkuName = cloudEnvironment.SkuName,
                Location = cloudEnvironment.Location,
            };

            var inputRequest = new List<AllocateRequestBody> { computeRequest };

            var resultResponse = await ResourceBrokerClient.AllocateAsync(Guid.Parse(cloudEnvironment.Id), inputRequest, logger);

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
            var computeRequest = new AllocateRequestBody
            {
                Type = ResourceType.ComputeVM,
                SkuName = cloudEnvironment.SkuName,
                Location = cloudEnvironment.Location,
            };

            var storageRequest = new AllocateRequestBody
            {
                Type = ResourceType.StorageFileShare,
                SkuName = cloudEnvironment.SkuName,
                Location = cloudEnvironment.Location,
            };

            var inputRequest = new List<AllocateRequestBody> { computeRequest, storageRequest };

            var resultResponse = await ResourceBrokerClient.AllocateAsync(Guid.Parse(cloudEnvironment.Id), inputRequest, logger);

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
            Requires.NotNull(startCloudEnvironmentParameters.FrontEndServiceUri, nameof(startCloudEnvironmentParameters.FrontEndServiceUri));

            var callbackUri = new Uri(string.Format(startCloudEnvironmentParameters.CallbackUriFormat, cloudEnvironment.Id));
            Requires.Argument(callbackUri.IsAbsoluteUri, nameof(callbackUri), "Must be an absolute URI.");

            var connectionToken = TokenProvider.GenerateEnvironmentConnectionToken(
                cloudEnvironment, startCloudEnvironmentParameters.UserProfile, logger);

            // Construct the start-compute environment variables
            var environmentVariables = EnvironmentVariableGenerator.Generate(
                cloudEnvironment,
                startCloudEnvironmentParameters.FrontEndServiceUri,
                callbackUri,
                connectionToken,
                cloudEnvironmentOptions);

            await ResourceBrokerClient.StartAsync(
                cloudEnvironment.Compute.ResourceId,
                new StartResourceRequestBody
                {
                    StorageResourceId = cloudEnvironment.Storage.ResourceId,
                    EnvironmentVariables = environmentVariables,
                },
                logger);
        }
    }
}
