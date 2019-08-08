﻿// <copyright file="DiagnosticsLoggerExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Logging extensions for <see cref="CloudEnvironment"/>.
    /// </summary>
    public static class DiagnosticsLoggerExtensions
    {
        private const string LogValueOwnerId = "OwnerId";
        private const string LogValueSessionId = "SessionId";
        private const string LogValueEnvironmentId = "EnvironmentRegistrationId";
        private const string LogValueComputeId = "ComputeId";
        private const string LogValueComputeTargetId = "ComputeTargetId";

        /// <summary>
        /// Add logging fields for an <see cref="CloudEnvironment"/> instance.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="environmentRegistration">The environment registration.</param>
        public static void AddRegistrationInfoToResponseLog(this IDiagnosticsLogger logger, CloudEnvironment environmentRegistration)
        {
            Requires.NotNull(logger, nameof(logger));
            Requires.NotNull(environmentRegistration, nameof(environmentRegistration));
            logger
                .AddEnvironmentId(environmentRegistration.Id)
                .AddOwnerId(environmentRegistration.OwnerId)
                .AddSessionId(environmentRegistration.Connection?.ConnectionSessionId)
                .AddComputeId(environmentRegistration.Connection?.ConnectionComputeId)
                .AddComputeTargetId(environmentRegistration.Connection?.ConnectionComputeTargetId);
        }

        /// <summary>
        /// Add an environment id to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="environmentId">The environment id.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddEnvironmentId(this IDiagnosticsLogger logger, string environmentId)
            => logger.FluentAddValue(LogValueEnvironmentId, environmentId);

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
        /// Add the environment connection compute target id to the logger.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="computeTargetId">The environment connection compute target id.</param>
        /// <returns>The <paramref name="logger"/>.</returns>
        public static IDiagnosticsLogger AddComputeTargetId(this IDiagnosticsLogger logger, string computeTargetId)
            => logger.FluentAddValue(LogValueComputeTargetId, computeTargetId);
    }
}
