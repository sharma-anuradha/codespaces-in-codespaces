// <copyright file="DeveloperStampDiagnosticsLogger.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.IO;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Audit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Developer.DevStampLogger
{
    /// <summary>
    /// Dev logger.
    /// </summary>
    public class DeveloperStampDiagnosticsLogger : IDiagnosticsLogger
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeveloperStampDiagnosticsLogger"/> class.
        /// </summary>
        /// <param name="baseValues">Base values.</param>
        /// <param name="stream">Text writer stream.</param>
        public DeveloperStampDiagnosticsLogger(LogValueSet baseValues, TextWriter stream)
        {
            JsonStdoutLogger = new JsonStdoutLogger(baseValues, stream);
        }

        private JsonStdoutLogger JsonStdoutLogger { get; set; }

        /// <inheritdoc/>
        public void AddBaseValue(string key, string value)
        {
            JsonStdoutLogger.AddBaseValue(key, value);
        }

        /// <inheritdoc/>
        public void AddValue(string key, string value)
        {
            JsonStdoutLogger.AddValue(key, value);
        }

        /// <inheritdoc/>
        public bool Audit(AuditScope scope, string operationName, AuditEventCategory category, CallerIdentity identity, TargetResource targetResource, OperationResult result)
        {
            return JsonStdoutLogger.Audit(scope, operationName, category, identity, targetResource, result);
        }

        /// <inheritdoc/>
        public bool Audit(AuditScope scope, AuditMandatoryProperties auditMandatoryProperties)
        {
            return JsonStdoutLogger.Audit(scope, auditMandatoryProperties);
        }

        /// <inheritdoc/>
        public void Log(string message, string level)
        {
            JsonStdoutLogger.Log(message, level);
        }

        /// <inheritdoc/>
        public void LogCritical(string message)
        {
            JsonStdoutLogger.LogCritical(message);
        }

        /// <inheritdoc/>
        public void LogError(string message)
        {
            JsonStdoutLogger.LogError(message);
        }

        /// <inheritdoc/>
        public void LogInfo(string message)
        {
            JsonStdoutLogger.LogInfo(message);
        }

        /// <inheritdoc/>
        public void LogWarning(string message)
        {
            JsonStdoutLogger.LogWarning(message);
        }

        /// <inheritdoc/>
        public IDiagnosticsLogger WithValue(string key, string value)
        {
            return JsonStdoutLogger.WithValue(key, value);
        }

        /// <inheritdoc/>
        public IDiagnosticsLogger WithValues(LogValueSet valueSet)
        {
            return JsonStdoutLogger.WithValues(valueSet);
        }
    }
}
