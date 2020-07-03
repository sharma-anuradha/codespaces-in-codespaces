﻿// <copyright file="EnvironmentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.RepairWorkflows;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;
using Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using SecretScopeModel = Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager.Contracts.SecretScope;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <inheritdoc/>
    public class EnvironmentManager : IEnvironmentManager
    {
        private const string LogBaseName = "environment_manager";

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentManager"/> class.
        /// </summary>
        /// <param name="cloudEnvironmentRepository">The cloud environment repository.</param>
        /// <param name="resourceBrokerHttpClient">The resource broker client.</param>
        /// <param name="tokenProvider">Provider capable of issuing access tokens.</param>
        /// <param name="skuCatalog">The SKU catalog.</param>
        /// <param name="environmentMonitor">The environment monitor.</param>
        /// <param name="environmentContinuation">The environment continuation.</param>
        /// <param name="environmentManagerSettings">The environment manager settings.</param>
        /// <param name="planManagerSettings">The plan manager settings.</param>
        /// <param name="environmentStateManager">The environment state manager.</param>
        /// <param name="environmentRepairWorkflows">The environment repair workflows.</param>
        /// <param name="resourceAllocationManager">The environment resource allocation manager.</param>
        /// <param name="workspaceManager">The workspace manager.</param>
        /// <param name="secretStoreManager">The secret store manager.</param>
        /// <param name="resourceSelector">Resource selector.</param>
        public EnvironmentManager(
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            ITokenProvider tokenProvider,
            ISkuCatalog skuCatalog,
            IEnvironmentMonitor environmentMonitor,
            IEnvironmentContinuationOperations environmentContinuation,
            EnvironmentManagerSettings environmentManagerSettings,
            PlanManagerSettings planManagerSettings,
            IEnvironmentStateManager environmentStateManager,
            IEnumerable<IEnvironmentRepairWorkflow> environmentRepairWorkflows,
            IResourceAllocationManager resourceAllocationManager,
            IWorkspaceManager workspaceManager,
            ISecretStoreManager secretStoreManager,
            IResourceSelectorFactory resourceSelector)
        {
            CloudEnvironmentRepository = Requires.NotNull(cloudEnvironmentRepository, nameof(cloudEnvironmentRepository));
            ResourceBrokerClient = Requires.NotNull(resourceBrokerHttpClient, nameof(resourceBrokerHttpClient));
            TokenProvider = Requires.NotNull(tokenProvider, nameof(tokenProvider));
            SkuCatalog = skuCatalog;
            EnvironmentMonitor = environmentMonitor;
            SecretStoreManager = Requires.NotNull(secretStoreManager, nameof(secretStoreManager));
            EnvironmentStateManager = Requires.NotNull(environmentStateManager, nameof(environmentStateManager));
            EnvironmentContinuation = Requires.NotNull(environmentContinuation, nameof(environmentContinuation));
            EnvironmentManagerSettings = Requires.NotNull(environmentManagerSettings, nameof(environmentManagerSettings));
            PlanManagerSettings = Requires.NotNull(planManagerSettings, nameof(PlanManagerSettings));
            EnvironmentRepairWorkflows = environmentRepairWorkflows.ToDictionary(x => x.WorkflowType);
            ResourceAllocationManager = Requires.NotNull(resourceAllocationManager, nameof(resourceAllocationManager));
            WorkspaceManager = Requires.NotNull(workspaceManager, nameof(workspaceManager));
            ResourceSelector = Requires.NotNull(resourceSelector, nameof(resourceSelector));
        }

        private ICloudEnvironmentRepository CloudEnvironmentRepository { get; }

        private ITokenProvider TokenProvider { get; }

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerClient { get; }

        private ISkuCatalog SkuCatalog { get; }

        private IEnvironmentMonitor EnvironmentMonitor { get; }

        private ISecretStoreManager SecretStoreManager { get; }

        private IEnvironmentStateManager EnvironmentStateManager { get; }

        private IEnvironmentContinuationOperations EnvironmentContinuation { get; }

        private EnvironmentManagerSettings EnvironmentManagerSettings { get; }

        private PlanManagerSettings PlanManagerSettings { get; }

        private Dictionary<EnvironmentRepairActions, IEnvironmentRepairWorkflow> EnvironmentRepairWorkflows { get; }

        private IResourceAllocationManager ResourceAllocationManager { get; }

        private IWorkspaceManager WorkspaceManager { get; }

        private IResourceSelectorFactory ResourceSelector { get; }

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

                    childLogger.AddCloudEnvironment(cloudEnvironment);
                    childLogger.AddVsoPlanInfo(cloudEnvironment.PlanId);

                    childLogger.FluentAddBaseValue("CloudEnvironmentOldState", originalState)
                        .FluentAddValue("CloudEnvironmentOldStateUpdated", cloudEnvironment.LastStateUpdated)
                        .FluentAddValue("CloudEnvironmentOldStateUpdatedTrigger", cloudEnvironment.LastStateUpdateTrigger)
                        .FluentAddValue("CloudEnvironmentOldStateUpdatedReason", cloudEnvironment.LastStateUpdateReason);

                    // TODO: Remove once Anu's update tracking is in.
                    // Check for an unavailable environment
                    switch (originalState)
                    {
                        // Remain in provisioning state until _callback is invoked.
                        case CloudEnvironmentState.Provisioning:

                            // Timeout if environment has stayed in provisioning state for more than an hour
                            var timeInProvisioningStateInMin = (DateTime.UtcNow - cloudEnvironment.LastStateUpdated).TotalMinutes;

                            childLogger.FluentAddBaseValue("CloudEnvironmentTimeInProvisioningStateInMin", timeInProvisioningStateInMin);

                            if (timeInProvisioningStateInMin > 60)
                            {
                                newState = CloudEnvironmentState.Failed;

                                childLogger.NewChildLogger()
                                    .LogErrorWithDetail($"{LogBaseName}_get_environment_state_refresh_error", $"Marking environment creation failed with timeout. Time in provisioning state {timeInProvisioningStateInMin} minutes.");
                            }

                            break;

                        // Swap between available and awaiting based on the workspace status
                        case CloudEnvironmentState.Available:
                        case CloudEnvironmentState.Awaiting:
                            var invitationId = cloudEnvironment.Connection?.ConnectionSessionId;
                            var workspace = await WorkspaceManager.GetWorkspaceStatusAsync(invitationId, childLogger.NewChildLogger());

                            childLogger.FluentAddBaseValue("CloudEnvironmentWorkspaceSet", workspace != null)
                                .FluentAddBaseValue("CloudEnvironmentIsHostConnectedHasValue", workspace?.IsHostConnected.HasValue)
                                .FluentAddBaseValue("CloudEnvironmentIsHostConnectedValue", workspace?.IsHostConnected);

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

                    childLogger.FluentAddBaseValue("CloudEnvironmentNewState", newState);

                    // Update the new state before returning.
                    if (originalState != newState)
                    {
                        await EnvironmentStateManager.SetEnvironmentStateAsync(cloudEnvironment, newState, CloudEnvironmentStateUpdateTriggers.GetEnvironment, null, null, childLogger.NewChildLogger());

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
        public Task<CloudEnvironment> UpdateAsync(
            CloudEnvironment cloudEnvironment,
            CloudEnvironmentState newState,
            string trigger,
            string reason,
            bool? isUserError,
            IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_update",
                async (childLogger) =>
                {
                    childLogger.AddCloudEnvironment(cloudEnvironment);
                    childLogger.AddVsoPlanInfo(cloudEnvironment.PlanId);

                    cloudEnvironment.Updated = DateTime.UtcNow;
                    if (newState != default && newState != cloudEnvironment.State)
                    {
                        await EnvironmentStateManager.SetEnvironmentStateAsync(cloudEnvironment, newState, trigger, reason, isUserError, childLogger.NewChildLogger());
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
                    childLogger.AddVsoPlanInfo(cloudEnvironment.PlanId);

                    cloudEnvironment.Connection.ConnectionSessionPath = options.Payload.SessionPath;

                    await EnvironmentStateManager.SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Available, CloudEnvironmentStateUpdateTriggers.EnvironmentCallback, string.Empty, null, childLogger.NewChildLogger());

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
            Subscription subscription,
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

            SkuCatalog.CloudEnvironmentSkus.TryGetValue(cloudEnvironment.SkuName, out var sku);

            return logger.OperationScopeAsync(
                $"{LogBaseName}_create",
                async (childLogger) =>
                {
                    childLogger.AddCloudEnvironment(cloudEnvironment);
                    childLogger.AddVsoPlanInfo(plan);

                    var result = new CloudEnvironmentServiceResult()
                    {
                        MessageCode = MessageCodes.Unknown,
                        HttpStatusCode = StatusCodes.Status409Conflict,
                    };

                    var environmentsInPlan = await ListAsync(childLogger.NewChildLogger(), planId: cloudEnvironment.PlanId);

                    // Validate against existing environments.
                    if (!IsEnvironmentNameAvailable(cloudEnvironment.FriendlyName, environmentsInPlan))
                    {
                        result.MessageCode = MessageCodes.EnvironmentNameAlreadyExists;
                        result.HttpStatusCode = StatusCodes.Status409Conflict;
                        return result;
                    }

                    if (subscription.IsBanned)
                    {
                        childLogger.LogError($"{LogBaseName}_create_subscriptionbanned_error");
                        result.MessageCode = MessageCodes.SubscriptionIsBanned;
                        result.HttpStatusCode = StatusCodes.Status403Forbidden;
                        return result;
                    }

                    var countOfEnvironmentsInPlan = environmentsInPlan.Count();
                    if (!(await CanEnvironmentFitInQuotaAsync(
                        cloudEnvironment, subscription, plan, countOfEnvironmentsInPlan, childLogger)))
                    {
                        childLogger.LogError($"{LogBaseName}_create_maxenvironmentsforplan_error");
                        result.MessageCode = MessageCodes.ExceededQuota;
                        result.HttpStatusCode = StatusCodes.Status403Forbidden;
                        return result;
                    }

                    // Setup
                    var environmentId = Guid.NewGuid();
                    cloudEnvironment.Id = environmentId.ToString();
                    cloudEnvironment.Created = cloudEnvironment.Updated = cloudEnvironment.LastUsed = DateTime.UtcNow;
                    cloudEnvironment.HasUnpushedGitChanges = false;

                    // Update CloudEnvironment telemetry property values now that Id, Created, Updated and LastUsed have been set.
                    childLogger.AddCloudEnvironment(cloudEnvironment);

                    // Static Environment
                    if (cloudEnvironment.Type == EnvironmentType.StaticEnvironment)
                    {
                        if (cloudEnvironment.Connection is null)
                        {
                            cloudEnvironment.Connection = new ConnectionInfo();
                        }

                        if (string.IsNullOrWhiteSpace(cloudEnvironment.Connection.ConnectionSessionId))
                        {
                            cloudEnvironment.Connection = await WorkspaceManager.CreateWorkspaceAsync(
                                EnvironmentType.StaticEnvironment,
                                cloudEnvironment.Id,
                                Guid.Empty,
                                startCloudEnvironmentParameters.ConnectionServiceUri,
                                cloudEnvironment.Connection?.ConnectionSessionPath,
                                startCloudEnvironmentParameters.UserProfile.Email,
                                null,
                                childLogger.NewChildLogger());
                        }

                        if (cloudEnvironment.Seed == default || cloudEnvironment.Seed.SeedType != SeedType.StaticEnvironment)
                        {
                            cloudEnvironment.Seed = new SeedInfo { SeedType = SeedType.StaticEnvironment };
                        }

                        cloudEnvironment.SkuName = StaticEnvironmentSku.Name;

                        // Update CloudEnvironment telemetry properties now that SessionId and SkuName have been updated.
                        childLogger.AddSessionId(cloudEnvironment.Connection?.ConnectionSessionId);
                        childLogger.AddSkuName(cloudEnvironment.SkuName);

                        // Environments must be initialized in Created state. But (at least for now) new environments immediately transition to Provisioning state.
                        await EnvironmentStateManager.SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Created, CloudEnvironmentStateUpdateTriggers.CreateEnvironment, string.Empty, null, childLogger.NewChildLogger());
                        await EnvironmentStateManager.SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Provisioning, CloudEnvironmentStateUpdateTriggers.CreateEnvironment, string.Empty, null, childLogger.NewChildLogger());

                        cloudEnvironment = await CloudEnvironmentRepository.CreateAsync(cloudEnvironment, childLogger.NewChildLogger());

                        result.CloudEnvironment = cloudEnvironment;
                        result.HttpStatusCode = StatusCodes.Status200OK;

                        try
                        {
                            var staticEnvironmentMonitoringEnabled = await EnvironmentManagerSettings.StaticEnvironmentMonitoringEnabled(childLogger);
                            if (staticEnvironmentMonitoringEnabled)
                            {
                                await EnvironmentMonitor.MonitorHeartbeatAsync(cloudEnvironment.Id, default(Guid), childLogger.NewChildLogger());
                            }
                        }
                        catch (Exception ex)
                        {
                            childLogger.LogException($"{LogBaseName}_create_monitor_error", ex);
                            result.MessageCode = MessageCodes.UnableToAllocateResources;
                            result.HttpStatusCode = StatusCodes.Status503ServiceUnavailable;
                            return result;
                        }

                        return result;
                    }

                    if (!IsValidSuspendTimeout(cloudEnvironment))
                    {
                        childLogger.LogError($"{LogBaseName}_create_invalidsuspendtimeout_error");

                        result.MessageCode = MessageCodes.RequestedAutoShutdownDelayMinutesIsInvalid;
                        result.HttpStatusCode = StatusCodes.Status400BadRequest;

                        return result;
                    }

                    if (cloudEnvironmentOptions.QueueResourceAllocation || !string.IsNullOrEmpty(cloudEnvironment.SubnetResourceId))
                    {
                        return await QueueCreateAsync(cloudEnvironment, cloudEnvironmentOptions, startCloudEnvironmentParameters, childLogger);
                    }

                    // Allocate Storage, Disk and Compute depending on the sku type.
                    try
                    {
                        var allocationResult = await AllocateRequiredResourcesAsync(cloudEnvironment, cloudEnvironmentOptions, childLogger.NewChildLogger());
                        cloudEnvironment.Storage = allocationResult.Storage;
                        cloudEnvironment.Compute = allocationResult.Compute;
                        cloudEnvironment.OSDisk = allocationResult.OSDisk;
                    }
                    catch (Exception ex) when (ex is RemoteInvocationException || ex is HttpResponseStatusException)
                    {
                        childLogger.LogError($"{LogBaseName}_create_allocate_error");

                        result.MessageCode = MessageCodes.UnableToAllocateResources;
                        result.HttpStatusCode = StatusCodes.Status503ServiceUnavailable;

                        return result;
                    }

                    // Update CloudEnvironment telemetry property values now that Storage, Compute, and OSDisk have been set.
                    childLogger.AddCloudEnvironment(cloudEnvironment);

                    // Start Environment Monitoring
                    try
                    {
                        await EnvironmentMonitor.MonitorHeartbeatAsync(cloudEnvironment.Id, cloudEnvironment.Compute.ResourceId, childLogger.NewChildLogger());
                    }
                    catch (Exception ex)
                    {
                        childLogger.LogException($"{LogBaseName}_create_monitor_error", ex);
                        result.MessageCode = MessageCodes.UnableToAllocateResources;
                        result.HttpStatusCode = StatusCodes.Status503ServiceUnavailable;

                        // Delete the allocated resources.
                        if (cloudEnvironment.Compute != null)
                        {
                            await ResourceBrokerClient.DeleteAsync(environmentId, cloudEnvironment.Compute.ResourceId, childLogger.NewChildLogger());
                        }

                        if (cloudEnvironment.OSDisk != null)
                        {
                            await ResourceBrokerClient.DeleteAsync(environmentId, cloudEnvironment.OSDisk.ResourceId, childLogger.NewChildLogger());
                        }

                        if (cloudEnvironment.Storage != null)
                        {
                            await ResourceBrokerClient.DeleteAsync(environmentId, cloudEnvironment.Storage.ResourceId, childLogger.NewChildLogger());
                        }

                        return result;
                    }

                    // Create the Live Share workspace
                    cloudEnvironment.Connection = await WorkspaceManager.CreateWorkspaceAsync(
                        EnvironmentType.CloudEnvironment,
                        cloudEnvironment.Id,
                        cloudEnvironment.Compute.ResourceId,
                        startCloudEnvironmentParameters.ConnectionServiceUri,
                        cloudEnvironment.Connection?.ConnectionSessionPath,
                        startCloudEnvironmentParameters.UserProfile.Email,
                        null,
                        childLogger.NewChildLogger());

                    // Create the cloud environment record in created state and transition immediately to the provisioning state -- before starting.
                    // This avoids a race condition where the record doesn't exist but the callback could be invoked.
                    // Highly unlikely, but still...
                    await EnvironmentStateManager.SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Created, CloudEnvironmentStateUpdateTriggers.CreateEnvironment, string.Empty, null, childLogger.NewChildLogger());
                    await EnvironmentStateManager.SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Provisioning, CloudEnvironmentStateUpdateTriggers.CreateEnvironment, string.Empty, null, childLogger.NewChildLogger());

                    // Persist core cloud environment record
                    cloudEnvironment = await CloudEnvironmentRepository.CreateAsync(cloudEnvironment, childLogger.NewChildLogger());

                    // Kick off start-compute before returning.
                    await StartComputeAsync(
                        cloudEnvironment,
                        cloudEnvironment.Compute.ResourceId,
                        cloudEnvironment.OSDisk?.ResourceId,
                        cloudEnvironment.Storage?.ResourceId,
                        null,
                        cloudEnvironmentOptions,
                        startCloudEnvironmentParameters,
                        childLogger.NewChildLogger());

                    result.CloudEnvironment = cloudEnvironment;
                    result.HttpStatusCode = StatusCodes.Status200OK;

                    // Kick off state transition monitoring.
                    try
                    {
                        await EnvironmentMonitor.MonitorProvisioningStateTransitionAsync(cloudEnvironment.Id, cloudEnvironment.Compute.ResourceId, childLogger.NewChildLogger());
                    }
                    catch (Exception ex)
                    {
                        childLogger.LogException($"{LogBaseName}_create_state_transition_monitor_error", ex);
                        throw;
                    }

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
                    childLogger.AddCloudEnvironment(cloudEnvironment);
                    childLogger.AddVsoPlanInfo(cloudEnvironment.PlanId);

                    await EnvironmentStateManager.SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Deleted, CloudEnvironmentStateUpdateTriggers.DeleteEnvironment, null, null, childLogger.NewChildLogger());

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

                                    await ResourceBrokerClient.DeleteAsync(
                                        Guid.Parse(cloudEnvironment.Id),
                                        storageIdToken.Value,
                                        innerLogger.NewChildLogger());
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

                                   await ResourceBrokerClient.DeleteAsync(
                                       Guid.Parse(cloudEnvironment.Id),
                                       computeIdToken.Value,
                                       innerLogger.NewChildLogger());
                               },
                               swallowException: true);
                        }

                        var osDiskIdToken = cloudEnvironment.OSDisk?.ResourceId;
                        if (osDiskIdToken != null)
                        {
                            await childLogger.OperationScopeAsync(
                               $"{LogBaseName}_delete_osdisk",
                               async (innerLogger) =>
                               {
                                   innerLogger.FluentAddBaseValue(nameof(cloudEnvironment.Id), cloudEnvironment.Id)
                                        .FluentAddBaseValue(nameof(osDiskIdToken), osDiskIdToken.Value);

                                   await ResourceBrokerClient.DeleteAsync(
                                       Guid.Parse(cloudEnvironment.Id),
                                       osDiskIdToken.Value,
                                       innerLogger.NewChildLogger());
                               },
                               swallowException: true);
                        }
                    }

                    if (cloudEnvironment.Connection?.WorkspaceId != null)
                    {
                        await childLogger.OperationScopeAsync(
                            $"{LogBaseName}_delete_workspace",
                            async (innerLogger) =>
                            {
                                innerLogger.FluentAddBaseValue(nameof(cloudEnvironment.Id), cloudEnvironment.Id)
                                    .FluentAddBaseValue("ConnectionSessionId", cloudEnvironment.Connection?.WorkspaceId);

                                await WorkspaceManager.DeleteWorkspaceAsync(cloudEnvironment.Connection.WorkspaceId, innerLogger.NewChildLogger());
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
            Subscription subscription,
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
                    childLogger.AddVsoPlanInfo(cloudEnvironment.PlanId);

                    if (subscription.SubscriptionState != SubscriptionStateEnum.Registered)
                    {
                        childLogger.LogError($"{LogBaseName}_resume_subscriptionstate_error");
                        return new CloudEnvironmentServiceResult
                        {
                            CloudEnvironment = cloudEnvironment,
                            MessageCode = MessageCodes.SubscriptionStateIsNotRegistered,
                            HttpStatusCode = StatusCodes.Status403Forbidden,
                        };
                    }

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

                    var sku = GetSku(cloudEnvironment);
                    var currentComputeUsed = await GetCurrentComputeUsedForSubscriptionAsync(subscription, sku, childLogger);
                    var computeCheckEnabled = await EnvironmentManagerSettings.ComputeCheckEnabled(childLogger.NewChildLogger());
                    var currentMaxQuota = subscription.CurrentMaximumQuota[sku.ComputeSkuFamily];
                    var windowsComputeCheckEnabled = await EnvironmentManagerSettings.WindowsComputeCheckEnabled(childLogger.NewChildLogger());
                    if (sku.ComputeOS == ComputeOS.Windows)
                    {
                        computeCheckEnabled = computeCheckEnabled && windowsComputeCheckEnabled;
                    }

                    if (computeCheckEnabled && (currentComputeUsed + sku.ComputeSkuCores > currentMaxQuota))
                    {
                        childLogger.AddValue("RequestedSku", sku.SkuName);
                        childLogger.AddValue("CurrentMaxQuota", currentMaxQuota.ToString());
                        childLogger.AddValue("CurrentComputeUsed", currentComputeUsed.ToString());
                        childLogger.AddSubscriptionId(subscription.Id);
                        childLogger.LogError($"{LogBaseName}_resume_exceed_compute_quota");

                        return new CloudEnvironmentServiceResult
                        {
                            MessageCode = MessageCodes.ExceededQuota,
                            HttpStatusCode = StatusCodes.Status403Forbidden,
                        };
                    }

                    if (!cloudEnvironment.IsShutdown())
                    {
                        return new CloudEnvironmentServiceResult
                        {
                            CloudEnvironment = null,
                            MessageCode = MessageCodes.EnvironmentNotShutdown,
                            HttpStatusCode = StatusCodes.Status400BadRequest,
                        };
                    }

                    var connectionWorkspaceRootId = cloudEnvironment.Connection?.WorkspaceId;
                    if (!string.IsNullOrWhiteSpace(connectionWorkspaceRootId))
                    {
                        // Delete the previous liveshare session from database.
                        // Do not block start process on delete of old workspace from liveshare db.
                        _ = Task.Run(() => WorkspaceManager.DeleteWorkspaceAsync(connectionWorkspaceRootId, childLogger.NewChildLogger()));
                        cloudEnvironment.Connection.ConnectionComputeId = null;
                        cloudEnvironment.Connection.ConnectionComputeTargetId = null;
                        cloudEnvironment.Connection.ConnectionServiceUri = null;
                        cloudEnvironment.Connection.ConnectionSessionId = null;
                        cloudEnvironment.Connection.WorkspaceId = null;
                    }

                    if (sku.ComputeOS == ComputeOS.Windows || !string.IsNullOrEmpty(cloudEnvironment.SubnetResourceId))
                    {
                        // Windows can only be queued resume because the VM has to be constructed from the given OS disk.
                        return await QueueResumeAsync(cloudEnvironment, startCloudEnvironmentParameters, childLogger.NewChildLogger());
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
                            await ResourceBrokerClient.DeleteAsync(Guid.Parse(cloudEnvironment.Id), cloudEnvironment.Compute.ResourceId, childLogger.NewChildLogger());
                        }

                        return new CloudEnvironmentServiceResult
                        {
                            MessageCode = MessageCodes.UnableToAllocateResourcesWhileStarting,
                            HttpStatusCode = StatusCodes.Status503ServiceUnavailable,
                        };
                    }

                    // Create the Live Share workspace
                    cloudEnvironment.Connection = await WorkspaceManager.CreateWorkspaceAsync(
                        EnvironmentType.CloudEnvironment,
                        cloudEnvironment.Id,
                        cloudEnvironment.Compute.ResourceId,
                        startCloudEnvironmentParameters.ConnectionServiceUri,
                        cloudEnvironment.Connection?.ConnectionSessionPath,
                        startCloudEnvironmentParameters.UserProfile.Email,
                        null,
                        childLogger.NewChildLogger());

                    // Setup variables for easier use
                    var computerResource = cloudEnvironment.Compute;
                    var storageResource = cloudEnvironment.Storage;
                    var osDiskResource = cloudEnvironment.OSDisk;
                    var archiveStorageResource = storageResource.Type == ResourceType.StorageArchive
                        ? storageResource : null;
                    var isArchivedEnvironment = archiveStorageResource != null;

                    childLogger.AddCloudEnvironmentIsArchived(isArchivedEnvironment);

                    // At this point, if archive record is going to be switched in it will have been
                    var startingStateReson = isArchivedEnvironment ? MessageCodes.RestoringFromArchive.ToString() : null;
                    await EnvironmentStateManager.SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Starting, CloudEnvironmentStateUpdateTriggers.StartEnvironment, startingStateReson, null, childLogger.NewChildLogger());

                    cloudEnvironment.Transitions.ShuttingDown.ResetStatus(true);

                    // Persist updates madee to date
                    await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, childLogger.NewChildLogger());

                    // Provision new storage if environment has been archvied but don't switch until complete
                    if (archiveStorageResource != null)
                    {
                        storageResource = await AllocateStorageAsync(cloudEnvironment, childLogger.NewChildLogger());
                    }

                    childLogger.AddStorageResourceId(storageResource?.ResourceId)
                        .AddArchiveStorageResourceId(archiveStorageResource?.ResourceId);

                    // Kick off start-compute before returning.
                    await StartComputeAsync(
                        cloudEnvironment,
                        computerResource.ResourceId,
                        osDiskResource?.ResourceId,
                        storageResource?.ResourceId,
                        archiveStorageResource?.ResourceId,
                        null,
                        startCloudEnvironmentParameters,
                        childLogger.NewChildLogger());

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
                            await ResourceBrokerClient.DeleteAsync(Guid.Parse(cloudEnvironment.Id), cloudEnvironment.Compute.ResourceId, childLogger.NewChildLogger());
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
        public Task<CloudEnvironment> ResumeCallbackAsync(
            CloudEnvironment cloudEnvironment,
            Guid storageResourceId,
            Guid? archiveStorageResourceId,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
            Requires.NotEmpty(storageResourceId, nameof(storageResourceId));
            Requires.NotNull(logger, nameof(logger));

            return logger.OperationScopeAsync(
                $"{LogBaseName}_resume_callback",
                async (childLogger) =>
                {
                    childLogger.AddCloudEnvironment(cloudEnvironment);
                    childLogger.AddVsoPlanInfo(cloudEnvironment.PlanId);

                    // Detect if environment is archived
                    var isEnvironmentIsArchived = cloudEnvironment.Storage.Type == ResourceType.StorageArchive;
                    var computeResourceId = cloudEnvironment.Compute.ResourceId;

                    childLogger.AddCloudEnvironmentIsArchived(isEnvironmentIsArchived)
                        .AddComputeResourceId(computeResourceId)
                        .AddStorageResourceId(storageResourceId)
                        .AddArchiveStorageResourceId(archiveStorageResourceId);

                    // Only need to trigger resume callback if environment was archived
                    if (isEnvironmentIsArchived && cloudEnvironment.Storage.Type == ResourceType.StorageArchive)
                    {
                        // Finalize start if we can
                        if (archiveStorageResourceId != null)
                        {
                            // Conduct update to swapout archived storage for file storage
                            await childLogger.RetryOperationScopeAsync(
                                $"{LogBaseName}_resume_callback_update",
                                async (retryLogger) =>
                                {
                                    // Fetch record so that we aren't updating the reference passed in
                                    cloudEnvironment = await CloudEnvironmentRepository.GetAsync(
                                        cloudEnvironment.Id, retryLogger.NewChildLogger());

                                    // Fetch resource details
                                    var storageDetails = await ResourceBrokerClient.StatusAsync(
                                        Guid.Parse(cloudEnvironment.Id), storageResourceId, retryLogger.NewChildLogger());

                                    // Switch out storage reference
                                    cloudEnvironment.Storage = new ResourceAllocationRecord
                                    {
                                        ResourceId = storageResourceId,
                                        Location = storageDetails.Location,
                                        SkuName = storageDetails.SkuName,
                                        Type = storageDetails.Type,
                                        Created = DateTime.UtcNow,
                                    };
                                    cloudEnvironment.Transitions.Archiving.ResetStatus(true);

                                    // Update record
                                    cloudEnvironment = await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, retryLogger.NewChildLogger());
                                });

                            // Delete archive blob once its not needed any more
                            await ResourceBrokerClient.DeleteAsync(Guid.Parse(cloudEnvironment.Id), archiveStorageResourceId.Value, childLogger.NewChildLogger());
                        }
                        else
                        {
                            throw new NotSupportedException("Failed to find necessary result and/or supporting data to complete restart.");
                        }
                    }

                    return cloudEnvironment;
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
                $"{LogBaseName}_suspend",
                async (childLogger) =>
                {
                    childLogger.AddCloudEnvironment(cloudEnvironment);
                    childLogger.AddVsoPlanInfo(cloudEnvironment.PlanId);

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

                    if (cloudEnvironment.IsShutdown())
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
                        await EnvironmentStateManager.SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.ShuttingDown, CloudEnvironmentStateUpdateTriggers.ShutdownEnvironment, null, null, childLogger.NewChildLogger());
                        cloudEnvironment.Transitions.Resuming.ResetStatus(true);

                        // Update the database state.
                        cloudEnvironment = await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, childLogger.NewChildLogger());

                        // Start the cleanup operation to shutdown environment.
                        await ResourceBrokerClient.SuspendAsync(Guid.Parse(cloudEnvironment.Id), cloudEnvironment.Compute.ResourceId, childLogger.NewChildLogger());

                        // Kick off state transition monitoring.
                        await EnvironmentMonitor.MonitorShutdownStateTransitionAsync(cloudEnvironment.Id, cloudEnvironment.Compute.ResourceId, childLogger.NewChildLogger());
                    }

                    return new CloudEnvironmentServiceResult
                    {
                        CloudEnvironment = cloudEnvironment,
                        HttpStatusCode = StatusCodes.Status200OK,
                    };
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
                $"{LogBaseName}_suspend_callback",
                async (childLogger) =>
                {
                    childLogger.AddCloudEnvironment(cloudEnvironment);
                    childLogger.AddVsoPlanInfo(cloudEnvironment.PlanId);

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

                    return await CleanupComputeAsync(cloudEnvironment, logger.NewChildLogger());
                });
        }

        /// <inheritdoc/>
        public async Task<CloudEnvironmentServiceResult> ForceSuspendAsync(
            CloudEnvironment cloudEnvironment,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
            Requires.NotNull(logger, nameof(logger));

            return await logger.OperationScopeAsync(
                $"{LogBaseName}_force_suspend",
                async (childLogger) =>
                {
                    await EnvironmentRepairWorkflows[EnvironmentRepairActions.ForceSuspend].ExecuteAsync(cloudEnvironment, childLogger);

                    return new CloudEnvironmentServiceResult
                    {
                        CloudEnvironment = cloudEnvironment,
                        HttpStatusCode = StatusCodes.Status200OK,
                    };
                },
                swallowException: true);
        }

        /// <inheritdoc/>
        public async Task<CloudEnvironmentUpdateResult> UpdateSettingsAsync(
            CloudEnvironment cloudEnvironment,
            CloudEnvironmentUpdate update,
            Subscription subscription,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
            Requires.NotNull(update, nameof(update));
            Requires.NotNull(logger, nameof(logger));

            return await logger.OperationScopeAsync(
                $"{LogBaseName}_update_settings",
                async (childLogger) =>
                {
                    childLogger.AddCloudEnvironment(cloudEnvironment);
                    childLogger.AddVsoPlanInfo(cloudEnvironment.PlanId);
                    childLogger.AddCloudEnvironmentUpdate(update);

                    var validationErrors = new List<MessageCodes>();
                    var transformActions = new List<Action<CloudEnvironment>>();

                    if (!cloudEnvironment.IsShutdown())
                    {
                        validationErrors.Add(MessageCodes.EnvironmentNotShutdown);
                    }
                    else
                    {
                        var allowedUpdates = await GetAvailableSettingsUpdatesAsync(cloudEnvironment, childLogger.NewChildLogger());

                        // Call all of the update handlers. They each return either
                        // a list of validation errors or an environment transform action.
                        // Thne collect those results (where non-null) into the two lists.
                        // The transform actions are not executed until after all validations.
                        var updateResults = new[]
                        {
                            UpdateAutoShutdownDelaySetting(update, allowedUpdates),
                            UpdateAllowedSkusSetting(update, allowedUpdates),
                            await UpdatePlanIdAndNameSettingAsync(
                                cloudEnvironment, update, subscription, childLogger),
                        };
                        foreach (var (messageCodes, transform) in updateResults)
                        {
                            if (messageCodes != null)
                            {
                                validationErrors.AddRange(messageCodes);
                            }

                            if (transform != null)
                            {
                                transformActions.Add(transform);
                            }
                        }
                    }

                    var originalPlanId = cloudEnvironment.PlanId;

                    if (!validationErrors.Any())
                    {
                        await Retry.DoAsync(
                            async (attempt) =>
                            {
                                if (attempt > 0)
                                {
                                    cloudEnvironment = await CloudEnvironmentRepository.GetAsync(
                                        cloudEnvironment.Id, childLogger.NewChildLogger());

                                    // Update in case a concurrent move request completed before this one.
                                    originalPlanId = cloudEnvironment.PlanId;
                                }

                                if (!cloudEnvironment.IsShutdown())
                                {
                                    validationErrors.Add(MessageCodes.EnvironmentNotShutdown);
                                    return;
                                }

                                // Apply all of the settings transform actions.
                                transformActions.ForEach((t) => t(cloudEnvironment));

                                cloudEnvironment.Updated = DateTime.UtcNow;

                                // Write the update to the DB. This will fail if something else modified
                                // the record since the current object was fetched; that's why there's retry.
                                cloudEnvironment = await CloudEnvironmentRepository.UpdateAsync(
                                    cloudEnvironment, childLogger.NewChildLogger());
                            });
                    }

                    if (validationErrors.Any())
                    {
                        childLogger.AddErrorDetail($"Error MessageCodes: [ {string.Join(", ", validationErrors)} ]");

                        return CloudEnvironmentUpdateResult.Error(validationErrors);
                    }

                    var currentState = cloudEnvironment.State;
                    if (cloudEnvironment.PlanId != originalPlanId)
                    {
                        // The plan was changed by one of the transforms. Emit a special "Moved"
                        // state-transition in the OLD plan. Another state-transition back to the
                        // current state will be emitted for the NEW plan.
                        var newPlanId = cloudEnvironment.PlanId;
                        cloudEnvironment.PlanId = originalPlanId;
                        await EnvironmentStateManager.SetEnvironmentStateAsync(
                            cloudEnvironment,
                            CloudEnvironmentState.Moved,
                            CloudEnvironmentStateUpdateTriggers.EnvironmentSettingsChanged,
                            reason: null,
                            isUserError: null,
                            logger.NewChildLogger());
                        cloudEnvironment.PlanId = newPlanId;
                    }

                    await EnvironmentStateManager.SetEnvironmentStateAsync(
                        cloudEnvironment,
                        currentState,
                        CloudEnvironmentStateUpdateTriggers.EnvironmentSettingsChanged,
                        reason: null,
                        isUserError: null,
                        childLogger.NewChildLogger());

                    return CloudEnvironmentUpdateResult.Success(cloudEnvironment);
                },
                swallowException: false);
        }

        /// <inheritdoc/>
        public async Task<CloudEnvironmentUpdateResult> UpdateFoldersListAsync(
            CloudEnvironment cloudEnvironment,
            CloudEnvironmentFolderBody update,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
            Requires.NotNull(update, nameof(update));
            Requires.NotNull(logger, nameof(logger));

            return await logger.OperationScopeAsync(
                $"{LogBaseName}_update_recent_folders",
                async (childLogger) =>
                {
                    var validationErrors = new List<MessageCodes>();

                    var validationDetails = new List<string>();

                    if (!(update.RecentFolderPaths is null))
                    {
                        if (update.RecentFolderPaths.Count > 20)
                        {
                            validationErrors.Add(MessageCodes.TooManyRecentFolders);
                        }
                        else
                        {
                            update.RecentFolderPaths.ForEach(path =>
                            {
                                if (path.Length > 1000)
                                {
                                    validationErrors.Add(MessageCodes.FilePathIsInvalid);
                                    validationDetails.Add(string.Join("-", MessageCodes.FilePathIsInvalid, string.Join("...", path.Substring(0, 30), path.Substring(path.Length - 30))));
                                }
                            });
                        }
                    }

                    if (validationErrors.Any())
                    {
                        childLogger.AddErrorDetail($"Error MessageCodes: [ {string.Join(", ", validationDetails)} ]");

                        return CloudEnvironmentUpdateResult.Error(validationErrors, string.Join(", ", validationDetails));
                    }

                    cloudEnvironment.RecentFolders = update.RecentFolderPaths;

                    cloudEnvironment = await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, childLogger.NewChildLogger());

                    return CloudEnvironmentUpdateResult.Success(cloudEnvironment);
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
                    childLogger.AddVsoPlanInfo(cloudEnvironment.PlanId);

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

        /// <inheritdoc/>
        public async Task<bool> StartComputeAsync(
            CloudEnvironment cloudEnvironment,
            Guid computeResourceId,
            Guid? osDiskResourceId,
            Guid? storageResourceId,
            Guid? archiveStorageResourceId,
            CloudEnvironmentOptions cloudEnvironmentOptions,
            StartCloudEnvironmentParameters startCloudEnvironmentParameters,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(startCloudEnvironmentParameters, nameof(startCloudEnvironmentParameters));
            Requires.NotNullOrEmpty(startCloudEnvironmentParameters.CallbackUriFormat, nameof(startCloudEnvironmentParameters.CallbackUriFormat));
            Requires.NotNull(startCloudEnvironmentParameters.FrontEndServiceUri, nameof(startCloudEnvironmentParameters.FrontEndServiceUri));
            var callbackUri = new Uri(string.Format(startCloudEnvironmentParameters.CallbackUriFormat, cloudEnvironment.Id));
            Requires.Argument(callbackUri.IsAbsoluteUri, nameof(callbackUri), "Must be an absolute URI.");
            if (!SkuCatalog.CloudEnvironmentSkus.TryGetValue(cloudEnvironment.SkuName, out var sku))
            {
                throw new ArgumentException($"Invalid SKU: {cloudEnvironment.SkuName}");
            }

            var connectionToken = await TokenProvider.GenerateEnvironmentConnectionTokenAsync(
                cloudEnvironment, sku, startCloudEnvironmentParameters.UserProfile, logger);

            // Construct the start-compute environment variables
            var environmentVariables = EnvironmentVariableGenerator.Generate(
                cloudEnvironment,
                startCloudEnvironmentParameters.FrontEndServiceUri,
                callbackUri,
                connectionToken,
                cloudEnvironmentOptions);

            // Construct the data for secret filtering
            var filterSecrets = await ConstructFilterSecretsDataAsync(cloudEnvironment, logger.NewChildLogger());

            // Setup input requests
            var resources = new List<StartRequestBody>
                {
                    new StartRequestBody
                    {
                        ResourceId = computeResourceId,
                        Variables = environmentVariables,
                        FilterSecrets = filterSecrets,
                    },
                };

            if (storageResourceId.HasValue)
            {
                resources.Add(new StartRequestBody
                {
                    ResourceId = storageResourceId.Value,
                });
            }

            if (osDiskResourceId.HasValue)
            {
                resources.Add(new StartRequestBody
                {
                    ResourceId = osDiskResourceId.Value,
                });
            }

            if (archiveStorageResourceId.HasValue)
            {
                resources.Add(new StartRequestBody
                {
                    ResourceId = archiveStorageResourceId.Value,
                });
            }

            // Execute start
            return await ResourceBrokerClient.StartAsync(
                 Guid.Parse(cloudEnvironment.Id),
                 StartRequestAction.StartCompute,
                 resources,
                 logger.NewChildLogger());
        }

        /// <inheritdoc/>
        public Task<IEnumerable<CloudEnvironment>> ListBySubscriptionAsync(Subscription subscription, IDiagnosticsLogger logger)
        {
            Requires.NotNull(logger, nameof(logger));
            Requires.NotNull(logger, nameof(subscription));

            return logger.OperationScopeAsync(
                $"{LogBaseName}_list_by_subscription",
                async (childLogger) =>
                {
                    return await CloudEnvironmentRepository.GetAllEnvironmentsInSubscriptionAsync(subscription.Id, logger.NewChildLogger());
                });
        }

        /// <summary>
        /// Checks if a name matches the name of any existing environments in a plan.
        /// </summary>
        /// <returns>
        /// Currently every name must be unique within the plan, even across multiple users.
        /// </returns>
        private static bool IsEnvironmentNameAvailable(
            string name,
            IEnumerable<CloudEnvironment> environmentsInPlan)
        {
            return !environmentsInPlan.Any(
                (env) => string.Equals(env.FriendlyName, name, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Checks if subscription / plan quotas allow adding the environment.
        /// </summary>
        private async Task<bool> CanEnvironmentFitInQuotaAsync(
            CloudEnvironment cloudEnvironment,
            Subscription subscription,
            VsoPlanInfo plan,
            int currentEnvironmentsInPlan,
            IDiagnosticsLogger logger)
        {
            var sku = GetSku(cloudEnvironment);
            bool computeCheckEnabled = cloudEnvironment.Type != EnvironmentType.StaticEnvironment &&
                await EnvironmentManagerSettings.ComputeCheckEnabled(logger.NewChildLogger());
            if (sku.ComputeOS == ComputeOS.Windows)
            {
                var windowsComputeCheckEnabled = await EnvironmentManagerSettings.WindowsComputeCheckEnabled(
                    logger.NewChildLogger());
                computeCheckEnabled = computeCheckEnabled && windowsComputeCheckEnabled;
            }

            if (computeCheckEnabled)
            {
                var currentComputeUsed = await GetCurrentComputeUsedForSubscriptionAsync(
                    subscription, sku, logger.NewChildLogger());
                var subscriptionComputeMaximum = subscription.CurrentMaximumQuota[sku.ComputeSkuFamily];
                if (currentComputeUsed + sku.ComputeSkuCores > subscriptionComputeMaximum)
                {
                    var currentMaxQuota = subscription.CurrentMaximumQuota[sku.ComputeSkuFamily];
                    logger.AddValue("RequestedSku", sku.SkuName);
                    logger.AddValue("CurrentMaxQuota", currentMaxQuota.ToString());
                    logger.AddValue("CurrentComputeUsed", currentComputeUsed.ToString());
                    logger.AddSubscriptionId(subscription.Id);
                    logger.LogError($"{LogBaseName}_create_exceed_compute_quota");
                    return false;
                }
            }
            else
            {
                var maxEnvironmentsForPlan = await EnvironmentManagerSettings.MaxEnvironmentsPerPlanAsync(
                    plan.Subscription, logger.NewChildLogger());
                if (currentEnvironmentsInPlan >= maxEnvironmentsForPlan)
                {
                    logger.LogError($"{LogBaseName}_create_maxenvironmentsforplan_error");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Applies an AutoShutdownDelay setting change to an environment.
        /// </summary>
        /// <returns>Either a list of validation errors or a transform action to be applied later.</returns>
        private (IEnumerable<MessageCodes>, Action<CloudEnvironment>) UpdateAutoShutdownDelaySetting(
            CloudEnvironmentUpdate update,
            CloudEnvironmentAvailableSettingsUpdates allowedUpdates)
        {
            if (update.AutoShutdownDelayMinutes.HasValue)
            {
                if (allowedUpdates.AllowedAutoShutdownDelayMinutes == null ||
                    !allowedUpdates.AllowedAutoShutdownDelayMinutes.Contains(update.AutoShutdownDelayMinutes.Value))
                {
                    return (new[] { MessageCodes.RequestedAutoShutdownDelayMinutesIsInvalid }, null);
                }
                else
                {
                    return (null, (cloudEnvironment) =>
                    {
                        cloudEnvironment.AutoShutdownDelayMinutes = update.AutoShutdownDelayMinutes.Value;
                    });
                }
            }

            return (null, null);
        }

        /// <summary>
        /// Applies a SKU setting change to an environment.
        /// </summary>
        /// <returns>Either a list of validation errors or a transform action to be applied later.</returns>
        private (IEnumerable<MessageCodes>, Action<CloudEnvironment>) UpdateAllowedSkusSetting(
            CloudEnvironmentUpdate update,
            CloudEnvironmentAvailableSettingsUpdates allowedUpdates)
        {
            if (!string.IsNullOrWhiteSpace(update.SkuName))
            {
                if (allowedUpdates.AllowedSkus == null || !allowedUpdates.AllowedSkus.Any())
                {
                    return (new[] { MessageCodes.UnableToUpdateSku }, null);
                }
                else if (!allowedUpdates.AllowedSkus.Any((sku) => sku.SkuName == update.SkuName))
                {
                    return (new[] { MessageCodes.RequestedSkuIsInvalid }, null);
                }
                else
                {
                    return (null, (cloudEnvironment) =>
                    {
                        // TODO - this assumes that the SKU change can be applied automatically on environment start.
                        // If the SKU change requires some other work then it should be applied here.
                        cloudEnvironment.SkuName = update.SkuName;
                    });
                }
            }

            return (null, null);
        }

        /// <summary>
        /// Applies name and/or plan changes to an environment.
        /// </summary>
        /// <returns>Either a list of validation errors or a transform action to be applied later.</returns>
        private async Task<(IEnumerable<MessageCodes>, Action<CloudEnvironment>)> UpdatePlanIdAndNameSettingAsync(
            CloudEnvironment cloudEnvironment,
            CloudEnvironmentUpdate update,
            Subscription subscription,
            IDiagnosticsLogger logger)
        {
            if (update.Plan != null && update.Plan.Plan.ResourceId != cloudEnvironment.PlanId)
            {
                Requires.NotNull(subscription, nameof(subscription));
                var validationErrors = new List<MessageCodes>();

                var destinationName = cloudEnvironment.FriendlyName;
                var environmentsInPlan = await ListAsync(logger.NewChildLogger(), planId: update.Plan.Plan.ResourceId);

                // Rename is handled specially when combined with moving, because the new name availability
                // must be checked in the new plan instead of the current plan.
                if (!string.IsNullOrWhiteSpace(update.FriendlyName) && update.FriendlyName != cloudEnvironment.FriendlyName)
                {
                    if (IsEnvironmentNameAvailable(update.FriendlyName, environmentsInPlan))
                    {
                        // The new name will be assigned to the cloudEnvironment after the Moved event.
                        destinationName = update.FriendlyName;
                    }
                    else
                    {
                        validationErrors.Add(MessageCodes.EnvironmentNameAlreadyExists);
                    }
                }

                if (update.Plan.Plan.Location != cloudEnvironment.Location)
                {
                    validationErrors.Add(MessageCodes.InvalidLocationChange);
                }

                if (!(await CanEnvironmentFitInQuotaAsync(
                    cloudEnvironment, subscription, update.Plan.Plan, environmentsInPlan.Count(), logger)))
                {
                    validationErrors.Add(MessageCodes.ExceededQuota);
                }

                // The returned action will only be invoked if there are no validation errors.
                return (validationErrors, (cloudEnvironment) =>
                {
                    cloudEnvironment.PlanId = update.Plan.Plan.ResourceId;
                    cloudEnvironment.FriendlyName = destinationName;
                });
            }
            else if (!string.IsNullOrWhiteSpace(update.FriendlyName) && update.FriendlyName != cloudEnvironment.FriendlyName)
            {
                var duplicateNamesInPlan = await ListAsync(
                    logger.NewChildLogger(), cloudEnvironment.PlanId, update.FriendlyName);
                if (!duplicateNamesInPlan.Any())
                {
                    return (null, (cloudEnvironment) =>
                    {
                        cloudEnvironment.FriendlyName = update.FriendlyName;
                    });
                }
                else
                {
                    return (new[] { MessageCodes.EnvironmentNameAlreadyExists }, null);
                }
            }

            return (null, null);
        }

        private async Task<CloudEnvironmentServiceResult> CleanupComputeAsync(
            CloudEnvironment cloudEnvironment,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LogBaseName}_cleanup_compute",
                async (childLogger) =>
                {
                    if (cloudEnvironment.OSDisk != default)
                    {
                        // Callbacks get triggered multiple times. We want to avoid queueing multiple continuations.
                        if (cloudEnvironment.Transitions?.ShuttingDown?.Status != Common.Continuation.OperationState.InProgress)
                        {
                            await EnvironmentContinuation.ShutdownAsync(
                                Guid.Parse(cloudEnvironment.Id),
                                false,
                                "Suspending",
                                logger.NewChildLogger());
                        }

                        // Clean up is handled by the shutdown environment continuation handler.
                        return new CloudEnvironmentServiceResult
                        {
                            CloudEnvironment = cloudEnvironment,
                            HttpStatusCode = StatusCodes.Status200OK,
                        };
                    }

                    var computeIdToken = cloudEnvironment.Compute?.ResourceId;

                    childLogger.FluentAddValue("ComputeResourceId", computeIdToken);

                    // Change environment state to shutdown if it is not already in shutdown state.
                    var shutdownState = CloudEnvironmentState.Shutdown;
                    if (cloudEnvironment.State != shutdownState)
                    {
                        await EnvironmentStateManager.SetEnvironmentStateAsync(cloudEnvironment, shutdownState, CloudEnvironmentStateUpdateTriggers.ShutdownEnvironment, null, null, logger);

                        await childLogger.RetryOperationScopeAsync(
                                    $"{LogBaseName}_cleanup_compute_record_update",
                                    async (retryLogger) =>
                                    {
                                        cloudEnvironment = await CloudEnvironmentRepository.GetAsync(cloudEnvironment.Id, logger.NewChildLogger());
                                        cloudEnvironment.State = shutdownState;
                                        cloudEnvironment.Compute = null;

                                        // Update the database state.
                                        cloudEnvironment = await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, childLogger.NewChildLogger());
                                    });
                    }

                    // Delete the allocated resources.
                    if (computeIdToken != default)
                    {
                        await ResourceBrokerClient.DeleteAsync(
                            Guid.Parse(cloudEnvironment.Id),
                            computeIdToken.Value,
                            childLogger.NewChildLogger());
                    }

                    return new CloudEnvironmentServiceResult
                    {
                        CloudEnvironment = cloudEnvironment,
                        HttpStatusCode = StatusCodes.Status200OK,
                    };
                });
        }

        private async Task<FilterSecretsBody> ConstructFilterSecretsDataAsync(CloudEnvironment cloudEnvironment, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LogBaseName}_construct_filter_secrets_data",
                async (childLogger) =>
                {
                    var planId = Requires.NotNull(cloudEnvironment.PlanId, nameof(cloudEnvironment.PlanId));
                    var filterSecretsBody = default(FilterSecretsBody);

                    var secretStores = await SecretStoreManager.GetAllSecretStoresByPlanAsync(planId, logger);
                    if (secretStores.Any())
                    {
                        var prioritizedSecretStoreResources = new List<PrioritizedSecretStoreResource>();
                        var planScopeSecretStore = secretStores.SingleOrDefault(secretStore => secretStore.Scope == SecretScopeModel.Plan &&
                                                                                               secretStore.SecretResource?.ResourceId != default &&
                                                                                               secretStore.SecretResource?.IsReady == true);
                        var userScopeSecretStore = secretStores.SingleOrDefault(secretStore => secretStore.Scope == SecretScopeModel.User &&
                                                                                               secretStore.SecretResource?.ResourceId != default &&
                                                                                               secretStore.SecretResource?.IsReady == true);

                        if (planScopeSecretStore != default)
                        {
                            prioritizedSecretStoreResources.Add(new PrioritizedSecretStoreResource
                            {
                                Priority = 2,
                                ResourceId = planScopeSecretStore.SecretResource.ResourceId,
                            });
                        }

                        if (userScopeSecretStore != default)
                        {
                            prioritizedSecretStoreResources.Add(new PrioritizedSecretStoreResource
                            {
                                Priority = 1,
                                ResourceId = userScopeSecretStore.SecretResource.ResourceId,
                            });
                        }

                        if (prioritizedSecretStoreResources.Any())
                        {
                            var secretFilterDataCollection = new List<SecretFilterData>();

                            // Add git repo filter data
                            secretFilterDataCollection.Add(new SecretFilterData
                            {
                                Type = SecretFilterType.GitRepo,
                                Data = cloudEnvironment.Seed?.SeedMoniker ?? string.Empty,
                            });

                            // Add codespace name filter data
                            secretFilterDataCollection.Add(new SecretFilterData
                            {
                                Type = SecretFilterType.CodespaceName,
                                Data = cloudEnvironment.FriendlyName,
                            });

                            filterSecretsBody = new FilterSecretsBody
                            {
                                FilterData = secretFilterDataCollection,
                                PrioritizedSecretStoreResources = prioritizedSecretStoreResources,
                            };
                        }
                    }

                    return filterSecretsBody;
                },
                swallowException: true);
        }

        private Task<CloudEnvironmentServiceResult> QueueCreateAsync(
           CloudEnvironment cloudEnvironment,
           CloudEnvironmentOptions cloudEnvironmentOptions,
           StartCloudEnvironmentParameters startCloudEnvironmentParameters,
           IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_queue_create",
                async (childLogger) =>
                {
                    childLogger.AddCloudEnvironment(cloudEnvironment);
                    childLogger.AddVsoPlanInfo(cloudEnvironment.PlanId);

                    // Initialize connection, if it is null, client will fail to get environment list.
                    cloudEnvironment.Connection = new ConnectionInfo();

                    // Create the cloud environment record in the provisioning state -- before starting.
                    // This avoids a race condition where the record doesn't exist but the callback could be invoked.
                    // Highly unlikely, but still...
                    await EnvironmentStateManager.SetEnvironmentStateAsync(
                        cloudEnvironment,
                        CloudEnvironmentState.Queued,
                        CloudEnvironmentStateUpdateTriggers.CreateEnvironment,
                        string.Empty,
                        null,
                        childLogger.NewChildLogger());

                    // Persist core cloud environment record
                    cloudEnvironment = await CloudEnvironmentRepository.CreateAsync(cloudEnvironment, childLogger.NewChildLogger());

                    await EnvironmentContinuation.CreateAsync(
                        Guid.Parse(cloudEnvironment.Id),
                        cloudEnvironment.LastStateUpdated,
                        cloudEnvironmentOptions,
                        startCloudEnvironmentParameters,
                        "createnewenvironment",
                        logger.NewChildLogger());

                    var result = new CloudEnvironmentServiceResult()
                    {
                        CloudEnvironment = cloudEnvironment,
                        HttpStatusCode = StatusCodes.Status200OK,
                    };

                    return result;
                },
                async (ex, childLogger) =>
                {
                    if (cloudEnvironment.Id != null)
                    {
                        await DeleteAsync(cloudEnvironment, childLogger.NewChildLogger());
                    }

                    return default(CloudEnvironmentServiceResult);
                });
        }

        private ICloudEnvironmentSku GetSku(CloudEnvironment cloudEnvironment)
        {
            if (!SkuCatalog.CloudEnvironmentSkus.TryGetValue(cloudEnvironment.SkuName, out var sku))
            {
                throw new ArgumentException($"Invalid SKU: {cloudEnvironment.SkuName}");
            }

            return sku;
        }

        private async Task<int> GetCurrentComputeUsedForSubscriptionAsync(Subscription subscription, ICloudEnvironmentSku desiredSku, IDiagnosticsLogger logger)
        {
            var allEnvs = await CloudEnvironmentRepository.GetAllEnvironmentsInSubscriptionAsync(subscription.Id, logger);
            var computeUsed = 0;
            foreach (var env in allEnvs)
            {
                if (IsEnvironmentInComputeUtilizingState(env))
                {
                    var sku = GetSku(env);
                    if (sku.ComputeSkuFamily.Equals(desiredSku.ComputeSkuFamily, StringComparison.OrdinalIgnoreCase))
                    {
                        computeUsed += sku.ComputeSkuCores;
                    }
                }
            }

            return computeUsed;
        }

        private bool IsEnvironmentInComputeUtilizingState(CloudEnvironment cloudEnvironment)
        {
            switch (cloudEnvironment.State)
            {
                case CloudEnvironmentState.None:
                case CloudEnvironmentState.Created:
                case CloudEnvironmentState.Queued:
                case CloudEnvironmentState.Provisioning:
                case CloudEnvironmentState.Available:
                case CloudEnvironmentState.Awaiting:
                case CloudEnvironmentState.Unavailable:
                case CloudEnvironmentState.Starting:
                case CloudEnvironmentState.ShuttingDown:
                    return true;
                case CloudEnvironmentState.Deleted:
                case CloudEnvironmentState.Shutdown:
                case CloudEnvironmentState.Archived:
                case CloudEnvironmentState.Failed:
                    return false;
                default:
                    return true;
            }
        }

        private Task<ResourceAllocationRecord> AllocateComputeAsync(
            CloudEnvironment cloudEnvironment,
            IDiagnosticsLogger logger)
        {
            return AllocateResourceAsync(cloudEnvironment, ResourceType.ComputeVM, logger);
        }

        private Task<CloudEnvironmentServiceResult> QueueResumeAsync(
           CloudEnvironment cloudEnvironment,
           StartCloudEnvironmentParameters startCloudEnvironmentParameters,
           IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_queue_resume",
                async (childLogger) =>
                {
                    childLogger.AddCloudEnvironment(cloudEnvironment);
                    childLogger.AddVsoPlanInfo(cloudEnvironment.PlanId);

                    // Initialize connection, if it is null, client will fail to get environment list.
                    cloudEnvironment.Connection = new ConnectionInfo();

                    // Create the cloud environment record in the provisioning state -- before starting.
                    // This avoids a race condition where the record doesn't exist but the callback could be invoked.
                    // Highly unlikely, but still...
                    await EnvironmentStateManager.SetEnvironmentStateAsync(
                        cloudEnvironment,
                        CloudEnvironmentState.Queued,
                        CloudEnvironmentStateUpdateTriggers.StartEnvironment,
                        string.Empty,
                        null,
                        childLogger.NewChildLogger());

                    cloudEnvironment.Transitions.ShuttingDown.ResetStatus(true);

                    // Persist core cloud environment record
                    cloudEnvironment = await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, childLogger.NewChildLogger());

                    await EnvironmentContinuation.ResumeAsync(
                        Guid.Parse(cloudEnvironment.Id),
                        cloudEnvironment.LastStateUpdated,
                        startCloudEnvironmentParameters,
                        "resumeenvironment",
                        logger.NewChildLogger());

                    var result = new CloudEnvironmentServiceResult()
                    {
                        CloudEnvironment = cloudEnvironment,
                        HttpStatusCode = StatusCodes.Status200OK,
                    };

                    return result;
                },
                async (ex, childLogger) =>
                {
                    await SuspendAsync(cloudEnvironment, childLogger.NewChildLogger());

                    return default(CloudEnvironmentServiceResult);
                });
        }

        private Task<ResourceAllocationRecord> AllocateStorageAsync(
            CloudEnvironment cloudEnvironment,
            IDiagnosticsLogger logger)
        {
            return AllocateResourceAsync(cloudEnvironment, ResourceType.StorageFileShare, logger);
        }

        private async Task<ResourceAllocationRecord> AllocateResourceAsync(
            CloudEnvironment cloudEnvironment,
            ResourceType resourceType,
            IDiagnosticsLogger logger)
        {
            var inputRequest = new AllocateRequestBody
            {
                Type = resourceType,
                SkuName = cloudEnvironment.SkuName,
                Location = cloudEnvironment.Location,
            };

            var resultResponse = await ResourceAllocationManager.AllocateResourcesAsync(
                Guid.Parse(cloudEnvironment.Id),
                new List<AllocateRequestBody>() { inputRequest },
                logger.NewChildLogger());

            return resultResponse.Single();
        }

        private async Task<ResourceAllocationResult> AllocateRequiredResourcesAsync(
            CloudEnvironment cloudEnvironment,
            CloudEnvironmentOptions cloudEnvironmentOptions,
            IDiagnosticsLogger logger)
        {
            var requests = await ResourceSelector.CreateAllocationRequestsAsync(cloudEnvironment, cloudEnvironmentOptions, logger);

            var resultResponse = await ResourceAllocationManager.AllocateResourcesAsync(
                Guid.Parse(cloudEnvironment.Id),
                requests,
                logger.NewChildLogger());

            var resourceAllocationResult = new ResourceAllocationResult()
            {
                Compute = resultResponse.SingleOrDefault(x => x.Type == ResourceType.ComputeVM),
                Storage = resultResponse.SingleOrDefault(x => x.Type == ResourceType.StorageFileShare),
                OSDisk = resultResponse.SingleOrDefault(x => x.Type == ResourceType.OSDisk),
            };

            return resourceAllocationResult;
        }

        private bool IsValidSuspendTimeout(CloudEnvironment cloudEnvironment)
        {
            if (cloudEnvironment == null)
            {
                return false;
            }

            return PlanManagerSettings
                .DefaultAutoSuspendDelayMinutesOptions
                .Contains(cloudEnvironment.AutoShutdownDelayMinutes);
        }
    }
}
