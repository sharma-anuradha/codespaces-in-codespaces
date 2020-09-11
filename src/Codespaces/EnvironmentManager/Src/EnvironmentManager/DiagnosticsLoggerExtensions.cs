// <copyright file="DiagnosticsLoggerExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Metrics;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Logging extensions for <see cref="CloudEnvironment"/>.
    /// </summary>
    public static class DiagnosticsLoggerExtensions
    {
        private const string LogValueCloudEnvironmentId = "CloudEnvironmentId";
        private const string LogValueSessionId = "SessionId";
        private const string LogValueNewSessionId = "NewSessionId";
        private const string LogValueWorkspaceId = "WorkspaceId";
        private const string LogValueNewWorkspaceId = "NewWorkspaceId";
        private const string LogValueComputeResourceId = "ComputeResourceId";
        private const string LogValueStorageResourceId = "StorageResourceId";
        private const string LogValueArchiveStorageResourceId = "ArchiveStorageResourceId";
        private const string LogValueOSDiskResourceId = "OSDiskResourceId";
        private const string LogValueCloudEnvironmentType = "CloudEnvironmentType";
        private const string LogValueCloudEnvironmentState = "CloudEnvironmentState";
        private const string LogValueCloudEnvironmentIsArchived = "CloudEnvironmentIsArchived";
        private const string LogValueAutoShutdownDelay = "AutoShutdownDelay";
        private const string LogValueSkuName = "SkuName";
        private const string LogValueLastStateUpdateReason = "LastStateUpdateReason";
        private const string LogValueLastStateUpdateTrigger = "LastStateUpdateTrigger";
        private const string LogValueTargetSkuName = "TargetSkuName";
        private const string LogValueTargetAutoShutdownDelay = "TargetAutoShutdownDelay";
        private const string LogValuePartner = "Partner";
        private const string LogValueCountryCode = "CountryCode";
        private const string LogValueAzureGeography = "AzureGeography";
        private const string LogValueVsoClientType = "VsoClientType";

        /// <summary>
        /// Add logging fields for an <see cref="CloudEnvironment"/> instance.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="cloudEnvironment">The cloud environment, or null.</param>
        /// <returns>The <paramref name="logger"/> instance.</returns>
        public static IDiagnosticsLogger AddCloudEnvironment(this IDiagnosticsLogger logger, CloudEnvironment cloudEnvironment)
        {
            Requires.NotNull(logger, nameof(logger));

            if (cloudEnvironment != null)
            {
                logger
                    .AddEnvironmentId(cloudEnvironment.Id)
                    .AddCloudEnvironmentType(cloudEnvironment.Type)
                    .AddSessionId(cloudEnvironment.Connection?.ConnectionSessionId)
                    .AddWorkspaceId(cloudEnvironment.Connection?.WorkspaceId)
                    .AddComputeResourceId(cloudEnvironment.Compute?.ResourceId)
                    .AddOSDiskResourceId(cloudEnvironment.OSDisk?.ResourceId)
                    .AddStorageResourceId(cloudEnvironment.Storage?.ResourceId)
                    .AddAutoShutdownDelay(cloudEnvironment.AutoShutdownDelayMinutes)
                    .AddSkuName(cloudEnvironment.SkuName)
                    .AddCloudEnvironmentState(cloudEnvironment.State)
                    .AddLastStateUpdateReason(cloudEnvironment.LastStateUpdateReason)
                    .AddLastStateUpdateTrigger(cloudEnvironment.LastStateUpdateTrigger)
                    .AddPartner(cloudEnvironment.Partner)
                    .AddCountryCode(cloudEnvironment.CreationMetrics?.IsoCountryCode)
                    .AddAzureGeography(cloudEnvironment.CreationMetrics?.AzureGeography)
                    .AddVsoClientType(cloudEnvironment.CreationMetrics?.VsoClientType);
            }

            return logger;
        }

        /// <summary>
        /// Add logging fields for ConnectionInfo instance.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="connectionInfo">The connection info.</param>
        /// <returns>The logger.</returns>
        public static IDiagnosticsLogger AddNewConnectionInfo(this IDiagnosticsLogger logger, ConnectionInfo connectionInfo)
        {
            Requires.NotNull(logger, nameof(logger));

            if (connectionInfo != default)
            {
                logger
                    .AddNewSessionId(connectionInfo?.ConnectionSessionId)
                    .AddNewWorkspaceId(connectionInfo?.WorkspaceId);
            }

            return logger;
        }

        /// <summary>
        /// Add an environment id to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="environmentId">The environment id.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddEnvironmentId(this IDiagnosticsLogger logger, string environmentId)
            => logger.FluentAddBaseValue(LogValueCloudEnvironmentId, environmentId);

        /// <summary>
        /// Add the environment connection session id to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="sessionId">The session id.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddSessionId(this IDiagnosticsLogger logger, string sessionId)
            => logger.FluentAddBaseValue(LogValueSessionId, sessionId);

        /// <summary>
        /// Add the environment connection invitation id to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="workspaceId">The workspace id.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddWorkspaceId(this IDiagnosticsLogger logger, string workspaceId)
            => logger.FluentAddBaseValue(LogValueWorkspaceId, workspaceId);

        /// <summary>
        /// Add the environment connection compute id to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="computeId">The environment connection compute id.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddComputeResourceId(this IDiagnosticsLogger logger, Guid? computeId)
            => logger.FluentAddBaseValue(LogValueComputeResourceId, computeId?.ToString());

        /// <summary>
        /// Add the environment osdisk id to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="osDiskId">The environment osdisk id.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddOSDiskResourceId(this IDiagnosticsLogger logger, Guid? osDiskId)
            => logger.FluentAddBaseValue(LogValueOSDiskResourceId, osDiskId?.ToString());

        /// <summary>
        /// Add the environment storage id to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="storageId">The environment connection storage id.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddStorageResourceId(this IDiagnosticsLogger logger, Guid? storageId)
            => logger.FluentAddBaseValue(LogValueStorageResourceId, storageId?.ToString());

        /// <summary>
        /// Add the environment archive storage id to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="archiveStorageId">The environment connection archive storage id.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddArchiveStorageResourceId(this IDiagnosticsLogger logger, Guid? archiveStorageId)
            => logger.FluentAddBaseValue(LogValueArchiveStorageResourceId, archiveStorageId?.ToString());

        /// <summary>
        /// Add the environment type to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="type">The cloud environment type.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddCloudEnvironmentType(this IDiagnosticsLogger logger, EnvironmentType type)
            => logger.FluentAddBaseValue(LogValueCloudEnvironmentType, type.ToString());

        /// <summary>
        /// Add the environment state to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="state">The cloud environment type.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddCloudEnvironmentState(this IDiagnosticsLogger logger, CloudEnvironmentState state)
            => logger.FluentAddBaseValue(LogValueCloudEnvironmentState, state.ToString());

        /// <summary>
        /// Add the environment archive state to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="isArchived"><c>true</c> if the cloud environment is archived; otherwise, <c>false</c>.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddCloudEnvironmentIsArchived(this IDiagnosticsLogger logger, bool isArchived)
            => logger.FluentAddValue(LogValueCloudEnvironmentIsArchived, isArchived);

        /// <summary>
        /// Add the environment state to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="autoShutdownDelay">The cloud environment auto shutdown delay in minutes.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddAutoShutdownDelay(this IDiagnosticsLogger logger, int? autoShutdownDelay)
            => logger.FluentAddValue(LogValueAutoShutdownDelay, autoShutdownDelay?.ToString());

        /// <summary>
        /// Add the environment SKU name to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="skuName">The cloud environment SKU name.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddSkuName(this IDiagnosticsLogger logger, string skuName)
            => logger.FluentAddBaseValue(LogValueSkuName, skuName);

        /// <summary>
        /// Add the environment state to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="reason">The cloud environment state change reason.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddLastStateUpdateReason(this IDiagnosticsLogger logger, string reason)
            => logger.FluentAddValue(LogValueLastStateUpdateReason, reason);

        /// <summary>
        /// Add the environment state trigger to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="trigger">The trigger that changed the state.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddLastStateUpdateTrigger(this IDiagnosticsLogger logger, string trigger)
            => logger.FluentAddValue(LogValueLastStateUpdateTrigger, trigger);

        /// <summary>
        /// Add the environment partner to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="value">The partner value.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddPartner(this IDiagnosticsLogger logger, Partner? value)
            => logger.FluentAddBaseValue(LogValuePartner, value);

        /// <summary>
        /// Add the environment country code to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="value">The country code value.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddCountryCode(this IDiagnosticsLogger logger, string value)
            => logger.FluentAddValue(LogValueCountryCode, value);

        /// <summary>
        /// Add the environment azure geo to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="value">The azure geo value.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddAzureGeography(this IDiagnosticsLogger logger, AzureGeography? value)
            => logger.FluentAddValue(LogValueAzureGeography, value);

        /// <summary>
        /// Add the environment vso client type to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="value">The azure geo value.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddVsoClientType(this IDiagnosticsLogger logger, VsoClientType? value)
            => logger.FluentAddValue(LogValueVsoClientType, value);

        /// <summary>
        /// Add the newly created environment connection session id to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="sessionId">The session id.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddNewSessionId(this IDiagnosticsLogger logger, string sessionId)
            => logger.FluentAddBaseValue(LogValueNewSessionId, sessionId);

        /// <summary>
        /// Add the newly environment connection invitation id to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="workspaceId">The workspace id.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddNewWorkspaceId(this IDiagnosticsLogger logger, string workspaceId)
            => logger.FluentAddBaseValue(LogValueNewWorkspaceId, workspaceId);

        /// <summary>
        /// Add logging fields for an <see cref="CloudEnvironmentUpdate"/> instance.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="cloudEnvironmentUpdate">The cloud environment update, or null.</param>
        /// <returns>The <paramref name="logger"/> instance.</returns>
        public static IDiagnosticsLogger AddCloudEnvironmentUpdate(this IDiagnosticsLogger logger, CloudEnvironmentUpdate cloudEnvironmentUpdate)
        {
            Requires.NotNull(logger, nameof(logger));

            if (cloudEnvironmentUpdate != null)
            {
                if (!string.IsNullOrWhiteSpace(cloudEnvironmentUpdate.SkuName))
                {
                    logger.FluentAddValue(LogValueTargetSkuName, cloudEnvironmentUpdate.SkuName);
                }

                if (cloudEnvironmentUpdate.AutoShutdownDelayMinutes.HasValue)
                {
                    logger.FluentAddValue(LogValueTargetAutoShutdownDelay, cloudEnvironmentUpdate.AutoShutdownDelayMinutes.Value);
                }
            }

            return logger;
        }
    }
}
