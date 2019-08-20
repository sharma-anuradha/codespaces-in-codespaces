// <copyright file="DiagnosticsLoggerExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Data;
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
        private const string LogValueComputeId = "ComputeId";
        private const string LogValueStorageId = "StorageId";
        private const string LogValueType = "Type";
        private const string LogValueState = "State";

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
                    .AddCloudEnvironmentState(cloudEnvironment.State)
                    .AddSessionId(cloudEnvironment.Connection?.ConnectionSessionId)
                    .AddComputeId(cloudEnvironment.Compute?.ResourceIdToken)
                    .AddStorageId(cloudEnvironment.Storage?.ResourceIdToken);
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
            => logger.FluentAddValue(LogValueCloudEnvironmentId, environmentId);

        /// <summary>
        /// Add the environment owner id to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="ownerId">The environment owner id.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddOwnerId(this IDiagnosticsLogger logger, string ownerId)
            => logger.FluentAddValue(LogValueOwnerId, ownerId);

        /// <summary>
        /// Add the environment connection session id to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="sessionId">The session id.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddSessionId(this IDiagnosticsLogger logger, string sessionId)
            => logger.FluentAddValue(LogValueSessionId, sessionId);

        /// <summary>
        /// Add the environment connection compute id to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="computeId">The environment connection compute id.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddComputeId(this IDiagnosticsLogger logger, string computeId)
            => logger.FluentAddValue(LogValueComputeId, computeId);

        /// <summary>
        /// Add the environment connection compute id to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="storageId">The environment connection storage id.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddStorageId(this IDiagnosticsLogger logger, string storageId)
            => logger.FluentAddValue(LogValueStorageId, storageId);

        /// <summary>
        /// Add the environment type to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="type">The cloud environment type.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddCloudEnvironmentType(this IDiagnosticsLogger logger, CloudEnvironmentType type)
            => logger.FluentAddValue(LogValueType, type.ToString());

        /// <summary>
        /// Add the environment state to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="state">The cloud environment type.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddCloudEnvironmentState(this IDiagnosticsLogger logger, CloudEnvironmentState state)
            => logger.FluentAddValue(LogValueState, state.ToString());
    }
}
