// <copyright file="MessageCodes.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// List error codes returned by <see cref="IEnvironmentManager"/>.
    /// </summary>
    public enum MessageCodes : int
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Quota exceeded.
        /// </summary>
        ExceededQuota = 1,

        /// <summary>
        /// Environment name specified already exists.
        /// </summary>
        EnvironmentNameAlreadyExists = 2,

        /// <summary>
        /// Cannot find the requested environment.
        /// </summary>
        EnvironmentDoesNotExist = 3,

        /// <summary>
        /// Cannot shutdown a static environment.
        /// </summary>
        ShutdownStaticEnvironment = 4,

        /// <summary>
        /// Cannot start a static environment.
        /// </summary>
        StartStaticEnvironment = 5,

        /// <summary>
        /// Environment is not available.
        /// </summary>
        EnvironmentNotAvailable = 6,

        /// <summary>
        /// Environment is not shutdown.
        /// </summary>
        EnvironmentNotShutdown = 7,

        /// <summary>
        /// Unable to allocate Storage or Compute resource from the pools.
        /// </summary>
        UnableToAllocateResources = 8,

        /// <summary>
        /// Unable to allocate Compute resource while starting a suspended environment.
        /// </summary>
        UnableToAllocateResourcesWhileStarting = 9,

        /// <summary>
        /// Unable to update an environment's AutoShutdownDelay setting because the requested value was invalid
        /// </summary>
        RequestedAutoShutdownDelayMinutesIsInvalid = 10,

        /// <summary>
        /// Unable to update an environment's SKU setting because the current SKU does not support transitions
        /// </summary>
        UnableToUpdateSku = 11,

        /// <summary>
        /// Unable to update an environment's SKU setting because the current SKU does not support the requested SKU
        /// </summary>
        RequestedSkuIsInvalid = 12,

        /// <summary>
        /// Environment heartbeat reported the environment as unhealthy.
        /// Generic message when we don't know the exact reason.
        /// </summary>
        HeartbeatUnhealthy = 13,

        /// <summary>
        /// Environment creation failed due to user provided information.
        /// Generic message when we don't know the exact reason.
        /// </summary>
        StartEnvironmentGenericError = 14,

        /// <summary>
        /// Restoring From Archive.
        /// </summary>
        RestoringFromArchive = 15,

        /// <summary>
        /// File path is too long.
        /// </summary>
        FilePathIsInvalid = 16,

        /// <summary>
        /// The list of file paths exceeds the maximum allowed number of elements
        /// </summary>
        TooManyRecentFolders = 17,

        /// <summary>
        /// The subscription has been banned.
        /// </summary>
        SubscriptionIsBanned = 18,

        /// <summary>
        /// Restoring From Archive.
        /// </summary>
        EnvironmentArchived = 19,

        /// <summary>
        /// The subscription state is not active.
        /// </summary>
        SubscriptionStateIsNotRegistered = 20,

        /// <summary>
        /// The feature is disabled.
        /// </summary>
        FeatureDisabled = 21,

        /// <summary>
        /// The subscription cannot create plans or codespaces.
        /// </summary>
        SubscriptionCannotPerformAction = 22,

        /// <summary>
        /// An environment cannot be moved to a different location.
        /// </summary>
        InvalidLocationChange = 23,

        /// <summary>
        /// Cannot find the requested plan.
        /// </summary>
        PlanDoesNotExist = 24,

        /// <summary>
        /// Unable to resolve the environment name conflict in restore.
        /// </summary>
        UnableToResolveEnvironmentNameConflict = 25,

        /// <summary>
        /// Exceeded secrets quota.
        /// </summary>
        ExceededSecretsQuota = 26,

        /// <summary>
        /// Tenant ID of the plan is invalid.
        /// </summary>
        InvalidPlanTenant = 27,

        /// <summary>
        /// Cannot export a static environment.
        /// </summary>
        ExportStaticEnvironment = 28,

        /// <summary>
        /// Environment exporting failed due to user provided information.
        /// Generic message when we don't know the exact reason.
        /// </summary>
        ExportEnvironmentGenericError = 29,

        /// <summary>
        /// The org devcontainer.json max length was exceeded.
        /// </summary>
        ExceededOrgDevContainerMaxLength = 30,

        /// <summary>
        /// This action is not allowed in the environment's current state.
        /// <summary>
        ActionNotAllowedInThisState = 31,
    }
}
