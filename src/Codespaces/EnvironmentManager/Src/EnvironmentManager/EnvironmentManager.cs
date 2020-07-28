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
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
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
        /// <param name="environmentContinuation">The environment continuation.</param>
        /// <param name="environmentManagerSettings">The environment manager settings.</param>
        /// <param name="planManager">The plan manager.</param>
        /// <param name="planManagerSettings">The plan manager settings.</param>
        /// <param name="environmentStateManager">The environment state manager.</param>
        /// <param name="resourceStartManager">The resource start manager.</param>
        /// <param name="environmentGetAction">Target environment get action.</param>
        /// <param name="environmentListAction">Target environment listaction.</param>
        /// <param name="environmentUpdateStatusAction">Target environment update status action.</param>
        /// <param name="environmentCreateAction">Target environment create action.</param>
        /// <param name="environmentResumeAction">Target environment resume action.</param>
        /// <param name="environmentFinalizeResumeAction">Target environment resume finalize action.</param>
        /// <param name="environmentSuspendAction">Target environment suspend action.</param>
        /// <param name="environmentForceSuspendAction">Target environment force suspend action.</param>
        /// <param name="environmentDeleteAction">Target environment delete action.</param>
        public EnvironmentManager(
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            ISkuCatalog skuCatalog,
            IEnvironmentContinuationOperations environmentContinuation,
            EnvironmentManagerSettings environmentManagerSettings,
            IPlanManager planManager,
            PlanManagerSettings planManagerSettings,
            IEnvironmentStateManager environmentStateManager,
            IResourceStartManager resourceStartManager,
            IEnvironmentGetAction environmentGetAction,
            IEnvironmentListAction environmentListAction,
            IEnvironmentUpdateStatusAction environmentUpdateStatusAction,
            IEnvironmentCreateAction environmentCreateAction,
            IEnvironmentResumeAction environmentResumeAction,
            IEnvironmentFinalizeResumeAction environmentFinalizeResumeAction,
            IEnvironmentSuspendAction environmentSuspendAction,
            IEnvironmentForceSuspendAction environmentForceSuspendAction,
            IEnvironmentDeleteAction environmentDeleteAction)
        {
            CloudEnvironmentRepository = Requires.NotNull(cloudEnvironmentRepository, nameof(cloudEnvironmentRepository));
            ResourceBrokerClient = Requires.NotNull(resourceBrokerHttpClient, nameof(resourceBrokerHttpClient));
            SkuCatalog = Requires.NotNull(skuCatalog, nameof(skuCatalog));
            EnvironmentContinuation = Requires.NotNull(environmentContinuation, nameof(environmentContinuation));
            EnvironmentManagerSettings = Requires.NotNull(environmentManagerSettings, nameof(environmentManagerSettings));
            PlanManager = Requires.NotNull(planManager, nameof(planManager));
            PlanManagerSettings = Requires.NotNull(planManagerSettings, nameof(PlanManagerSettings));
            EnvironmentStateManager = Requires.NotNull(environmentStateManager, nameof(environmentStateManager));
            ResourceStartManager = Requires.NotNull(resourceStartManager, nameof(resourceStartManager));
            EnvironmentGetAction = Requires.NotNull(environmentGetAction, nameof(environmentGetAction));
            EnvironmentListAction = Requires.NotNull(environmentListAction, nameof(environmentListAction));
            EnvironmentUpdateStatusAction = Requires.NotNull(environmentUpdateStatusAction, nameof(environmentUpdateStatusAction));
            EnvironmentCreateAction = Requires.NotNull(environmentCreateAction, nameof(environmentCreateAction));
            EnvironmentResumeAction = Requires.NotNull(environmentResumeAction, nameof(environmentResumeAction));
            EnvironmentFinalizeResumeAction = Requires.NotNull(environmentFinalizeResumeAction, nameof(environmentFinalizeResumeAction));
            EnvironmentSuspendAction = Requires.NotNull(environmentSuspendAction, nameof(environmentSuspendAction));
            EnvironmentForceSuspendAction = Requires.NotNull(environmentForceSuspendAction, nameof(environmentForceSuspendAction));
            EnvironmentDeleteAction = Requires.NotNull(environmentDeleteAction, nameof(environmentDeleteAction));
        }

        private ICloudEnvironmentRepository CloudEnvironmentRepository { get; }

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerClient { get; }

        private ISkuCatalog SkuCatalog { get; }

        private IEnvironmentStateManager EnvironmentStateManager { get; }

        private IEnvironmentContinuationOperations EnvironmentContinuation { get; }

        private EnvironmentManagerSettings EnvironmentManagerSettings { get; }

        private IPlanManager PlanManager { get; }

        private PlanManagerSettings PlanManagerSettings { get; }

        private IResourceStartManager ResourceStartManager { get; }

        private IEnvironmentGetAction EnvironmentGetAction { get; }

        private IEnvironmentListAction EnvironmentListAction { get; }

        private IEnvironmentUpdateStatusAction EnvironmentUpdateStatusAction { get; }

        private IEnvironmentCreateAction EnvironmentCreateAction { get; }

        private IEnvironmentResumeAction EnvironmentResumeAction { get; }

        private IEnvironmentFinalizeResumeAction EnvironmentFinalizeResumeAction { get; }

        private IEnvironmentSuspendAction EnvironmentSuspendAction { get; }

        private IEnvironmentForceSuspendAction EnvironmentForceSuspendAction { get; }

        private IEnvironmentDeleteAction EnvironmentDeleteAction { get; }

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
            Guid environmentId,
            IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(environmentId, nameof(environmentId));
            Requires.NotNull(logger, nameof(logger));

            return EnvironmentDeleteAction.Run(environmentId, logger);
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> ResumeAsync(
            Guid environmentId,
            StartCloudEnvironmentParameters startCloudEnvironmentParameters,
            IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(environmentId, nameof(environmentId));
            Requires.NotNull(logger, nameof(logger));
            Requires.NotNull(startCloudEnvironmentParameters, nameof(startCloudEnvironmentParameters));

            return EnvironmentResumeAction.Run(environmentId, startCloudEnvironmentParameters, logger);
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> ResumeCallbackAsync(
            Guid environmentId,
            Guid storageResourceId,
            Guid? archiveStorageResourceId,
            IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(environmentId, nameof(environmentId));
            Requires.NotEmpty(storageResourceId, nameof(storageResourceId));
            Requires.NotNull(logger, nameof(logger));

            return EnvironmentFinalizeResumeAction.Run(environmentId, storageResourceId, archiveStorageResourceId, logger);
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> SuspendAsync(
            Guid environmentId,
            IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(environmentId, nameof(environmentId));
            Requires.NotNull(logger, nameof(logger));

            return EnvironmentSuspendAction.Run(environmentId, logger);
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
        public Task<CloudEnvironment> ForceSuspendAsync(
            Guid environmentId,
            IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(environmentId, nameof(environmentId));
            Requires.NotNull(logger, nameof(logger));

            return EnvironmentForceSuspendAction.Run(environmentId, logger);
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
    }
}
