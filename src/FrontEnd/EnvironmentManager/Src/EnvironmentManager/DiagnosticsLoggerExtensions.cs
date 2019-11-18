// <copyright file="DiagnosticsLoggerExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Logging extensions for <see cref="CloudEnvironment"/>.
    /// </summary>
    public static class DiagnosticsLoggerExtensions
    {
        private const string LogValueCloudEnvironmentId = "CloudEnvironmentId";
        private const string LogValueOwnerId = "OwnerId";
        private const string LogValueSessionId = "SessionId";
        private const string LogValueComputeResourceId = "ComputeResourceId";
        private const string LogValueStorageResourceId = "StorageResourceId";
        private const string LogValueCloudEnvironmentType = "CloudEnvironmentType";
        private const string LogValueCloudEnvironmentState = "CloudEnvironmentState";
        private const string LogValueAutoShutdownDelay = "AutoShutdownDelay";
        private const string LogValueLastStateUpdateReason = "LastStateUpdateReason";

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
                    .AddOwnerId(cloudEnvironment.OwnerId)
                    .AddCloudEnvironmentType(cloudEnvironment.Type)
                    .AddSessionId(cloudEnvironment.Connection?.ConnectionSessionId)
                    .AddComputeResourceId(cloudEnvironment.Compute?.ResourceId)
                    .AddStorageResourceId(cloudEnvironment.Storage?.ResourceId)
                    .AddAutoShutdownDelay(cloudEnvironment.AutoShutdownDelayMinutes)
                    .AddCloudEnvironmentState(cloudEnvironment.State)
                    .AddLastStateUpdateReason(cloudEnvironment.LastStateUpdateReason);
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
        /// Add the environment owner id to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="ownerId">The environment owner id.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddOwnerId(this IDiagnosticsLogger logger, string ownerId)
            => logger.FluentAddBaseValue(LogValueOwnerId, ownerId);

        /// <summary>
        /// Add the environment connection session id to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="sessionId">The session id.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddSessionId(this IDiagnosticsLogger logger, string sessionId)
            => logger.FluentAddBaseValue(LogValueSessionId, sessionId);

        /// <summary>
        /// Add the environment connection compute id to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="computeId">The environment connection compute id.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddComputeResourceId(this IDiagnosticsLogger logger, Guid? computeId)
            => logger.FluentAddBaseValue(LogValueComputeResourceId, computeId?.ToString());

        /// <summary>
        /// Add the environment connection compute id to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="storageId">The environment connection storage id.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddStorageResourceId(this IDiagnosticsLogger logger, Guid? storageId)
            => logger.FluentAddBaseValue(LogValueStorageResourceId, storageId.ToString());

        /// <summary>
        /// Add the environment type to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="type">The cloud environment type.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddCloudEnvironmentType(this IDiagnosticsLogger logger, CloudEnvironmentType type)
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
        /// Add the environment state to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="autoShutdownDelay">The cloud environment auto shutdown delay in minutes.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddAutoShutdownDelay(this IDiagnosticsLogger logger, int? autoShutdownDelay)
            => logger.FluentAddValue(LogValueAutoShutdownDelay, autoShutdownDelay?.ToString());

        /// <summary>
        /// Add the environment state to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="reason">The cloud environment state change reason.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddLastStateUpdateReason(this IDiagnosticsLogger logger, string reason)
            => logger.FluentAddValue(LogValueLastStateUpdateReason, reason);
    }
}
