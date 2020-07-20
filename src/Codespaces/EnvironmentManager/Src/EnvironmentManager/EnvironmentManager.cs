// <copyright file="EnvironmentManager.cs" company="Microsoft">
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
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.RepairWorkflows;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;

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
        /// <param name="skuCatalog">The SKU catalog.</param>
        /// <param name="environmentMonitor">The environment monitor.</param>
        /// <param name="environmentContinuation">The environment continuation.</param>
        /// <param name="environmentManagerSettings">The environment manager settings.</param>
        /// <param name="planManager">The plan manager.</param>
        /// <param name="planManagerSettings">The plan manager settings.</param>
        /// <param name="environmentStateManager">The environment state manager.</param>
        /// <param name="environmentRepairWorkflows">The environment repair workflows.</param>
        /// <param name="resourceAllocationManager">The resource allocation manager.</param>
        /// <param name="resourceStartManager">The resource start manager.</param>
        /// <param name="workspaceManager">The workspace manager.</param>
        /// <param name="environmentGetAction">Target environment get action.</param>
        /// <param name="environmentListAction">Target environment listaction.</param>
        /// <param name="environmentUpdateStatusAction">Target environment update status action.</param>
        /// <param name="environmentCreateAction">Target environment create action.</param>
        /// <param name="subscriptionManager">The subscription manager.</param>
        /// <param name="resourceSelector">Resource selector.</param>
        public EnvironmentManager(
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            ISkuCatalog skuCatalog,
            IEnvironmentMonitor environmentMonitor,
            IEnvironmentContinuationOperations environmentContinuation,
            EnvironmentManagerSettings environmentManagerSettings,
            IPlanManager planManager,
            PlanManagerSettings planManagerSettings,
            IEnvironmentStateManager environmentStateManager,
            IEnumerable<IEnvironmentRepairWorkflow> environmentRepairWorkflows,
            IResourceAllocationManager resourceAllocationManager,
            IResourceStartManager resourceStartManager,
            IWorkspaceManager workspaceManager,
            IEnvironmentGetAction environmentGetAction,
            IEnvironmentListAction environmentListAction,
            IEnvironmentUpdateStatusAction environmentUpdateStatusAction,
            IEnvironmentCreateAction environmentCreateAction)
        {
            CloudEnvironmentRepository = Requires.NotNull(cloudEnvironmentRepository, nameof(cloudEnvironmentRepository));
            ResourceBrokerClient = Requires.NotNull(resourceBrokerHttpClient, nameof(resourceBrokerHttpClient));
            SkuCatalog = skuCatalog;
            EnvironmentMonitor = environmentMonitor;
            EnvironmentStateManager = Requires.NotNull(environmentStateManager, nameof(environmentStateManager));
            EnvironmentContinuation = Requires.NotNull(environmentContinuation, nameof(environmentContinuation));
            EnvironmentManagerSettings = Requires.NotNull(environmentManagerSettings, nameof(environmentManagerSettings));
            PlanManager = Requires.NotNull(planManager, nameof(planManager));
            PlanManagerSettings = Requires.NotNull(planManagerSettings, nameof(PlanManagerSettings));
            EnvironmentRepairWorkflows = environmentRepairWorkflows.ToDictionary(x => x.WorkflowType);
            ResourceAllocationManager = Requires.NotNull(resourceAllocationManager, nameof(resourceAllocationManager));
            ResourceStartManager = Requires.NotNull(resourceStartManager, nameof(resourceStartManager));
            WorkspaceManager = Requires.NotNull(workspaceManager, nameof(workspaceManager));
            EnvironmentGetAction = Requires.NotNull(environmentGetAction, nameof(environmentGetAction));
            EnvironmentListAction = Requires.NotNull(environmentListAction, nameof(environmentListAction));
            EnvironmentUpdateStatusAction = Requires.NotNull(environmentUpdateStatusAction, nameof(environmentUpdateStatusAction));
            EnvironmentCreateAction = Requires.NotNull(environmentCreateAction, nameof(environmentCreateAction));
        }

        private ICloudEnvironmentRepository CloudEnvironmentRepository { get; }

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerClient { get; }

        private ISkuCatalog SkuCatalog { get; }

        private IEnvironmentMonitor EnvironmentMonitor { get; }

        private IEnvironmentStateManager EnvironmentStateManager { get; }

        private IEnvironmentContinuationOperations EnvironmentContinuation { get; }

        private EnvironmentManagerSettings EnvironmentManagerSettings { get; }

        private IPlanManager PlanManager { get; }

        private PlanManagerSettings PlanManagerSettings { get; }

        private Dictionary<EnvironmentRepairActions, IEnvironmentRepairWorkflow> EnvironmentRepairWorkflows { get; }

        private IResourceAllocationManager ResourceAllocationManager { get; }

        private IResourceStartManager ResourceStartManager { get; }

        private IWorkspaceManager WorkspaceManager { get; }

        private IEnvironmentGetAction EnvironmentGetAction { get; }

        private IEnvironmentListAction EnvironmentListAction { get; }

        private IEnvironmentUpdateStatusAction EnvironmentUpdateStatusAction { get; }

        private IEnvironmentCreateAction EnvironmentCreateAction { get; }

        /// <inheritdoc/>
        public Task<CloudEnvironment> GetAsync(
            Guid id,
            IDiagnosticsLogger logger)
        {
            return EnvironmentGetAction.Run(id, logger.NewChildLogger());
        }

        /// <inheritdoc/>
        public Task<IEnumerable<CloudEnvironment>> ListAsync(
            string planId,
            string environmentName,
            UserIdSet userIdSet,
            IDiagnosticsLogger logger)
        {
            return EnvironmentListAction.Run(planId, environmentName, userIdSet, logger);
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> UpdateStatusAsync(
            Guid cloudEnvironmentId,
            CloudEnvironmentState newState,
            string trigger,
            string reason,
            IDiagnosticsLogger logger)
        {
            return EnvironmentUpdateStatusAction.Run(cloudEnvironmentId, newState, trigger, reason, logger);
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
        public Task<CloudEnvironment> CreateAsync(
            EnvironmentCreateDetails details,
            StartCloudEnvironmentParameters startEnvironmentParams,
            MetricsInfo metricsInfo,
            IDiagnosticsLogger logger)
        {
            return EnvironmentCreateAction.Run(details, startEnvironmentParams, metricsInfo, logger);
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
                    var computeCheckEnabled = (sku.ComputeOS == ComputeOS.Windows) ? await EnvironmentManagerSettings.WindowsComputeCheckEnabled(childLogger.NewChildLogger()) : true;
                    var currentMaxQuota = subscription.CurrentMaximumQuota[sku.ComputeSkuFamily];

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
                    await ResourceStartManager.StartComputeAsync(
                        cloudEnvironment, computerResource.ResourceId, osDiskResource?.ResourceId, storageResource?.ResourceId, archiveStorageResource?.ResourceId, null, startCloudEnvironmentParameters, childLogger.NewChildLogger());

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
        public Task<bool> StartComputeAsync(
            CloudEnvironment cloudEnvironment,
            Guid computeResourceId,
            Guid? osDiskResourceId,
            Guid? storageResourceId,
            Guid? archiveStorageResourceId,
            CloudEnvironmentOptions cloudEnvironmentOptions,
            StartCloudEnvironmentParameters startCloudEnvironmentParameters,
            IDiagnosticsLogger logger)
        {
            return ResourceStartManager.StartComputeAsync(
                cloudEnvironment, computeResourceId, osDiskResourceId, storageResourceId, archiveStorageResourceId, null, startCloudEnvironmentParameters, logger.NewChildLogger());
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
                var environmentsInPlan = await ListAsync(update.Plan.Plan.ResourceId, null, null, logger.NewChildLogger());

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

                var currentPlanInfo = VsoPlanInfo.TryParse(cloudEnvironment.PlanId);
                VsoPlan currentPlan = currentPlanInfo == null ? null :
                    await PlanManager.GetAsync(currentPlanInfo, logger);

                // The returned action will only be invoked if there are no validation errors.
                return (validationErrors, (CloudEnvironment cloudEnvironment) =>
                {
                    if (currentPlan != null &&
                        cloudEnvironment.OwnerId.StartsWith(currentPlan.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        // The owner ID uses a plan-level tenant. (It's a plan-scoped delegated identity.)
                        // Update it to the new plan ID.
                        logger.LogInfo($"{LogBaseName}_update_environment_ownerid");
                        cloudEnvironment.OwnerId =
                            update.Plan.Id + cloudEnvironment.OwnerId.Substring(currentPlan.Id.Length);
                    }

                    cloudEnvironment.PlanId = update.Plan.Plan.ResourceId;
                    cloudEnvironment.FriendlyName = destinationName;
                });
            }
            else if (!string.IsNullOrWhiteSpace(update.FriendlyName) && update.FriendlyName != cloudEnvironment.FriendlyName)
            {
                var duplicateNamesInPlan = await ListAsync(cloudEnvironment.PlanId, update.FriendlyName, null, logger.NewChildLogger());
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
                        await EnvironmentStateManager.SetEnvironmentStateAsync(
                            cloudEnvironment,
                            shutdownState,
                            CloudEnvironmentStateUpdateTriggers.ShutdownEnvironment,
                            null,
                            null,
                            logger);

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
    }
}
