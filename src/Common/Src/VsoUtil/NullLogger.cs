// <copyright file="NullLogger.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Audit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil
{
    /// <inheritdoc/>
    public class NullLogger : IDiagnosticsLogger
    {
        /// <inheritdoc/>
        public void AddBaseValue(string key, string value)
        {
        }

        /// <inheritdoc/>
        public void AddValue(string key, string value)
        {
        }

        /// <inheritdoc/>
        public bool Audit(AuditScope scope, string operationName, AuditEventCategory category, CallerIdentity identity, TargetResource targetResource, OperationResult result)
        {
            return true;
        }

        /// <inheritdoc/>
        public bool Audit(AuditScope scope, AuditMandatoryProperties auditMandatoryProperties)
        {
            return true;
        }

        /// <inheritdoc/>
        public void Log(string message, string level)
        {
        }

        /// <inheritdoc/>
        public void LogCritical(string message)
        {
        }

        /// <inheritdoc/>
        public void LogError(string message)
        {
        }

        /// <inheritdoc/>
        public void LogInfo(string message)
        {
        }

        /// <inheritdoc/>
        public void LogWarning(string message)
        {
        }

        /// <inheritdoc/>
        public IDiagnosticsLogger WithValue(string key, string value)
        {
            return this;
        }

        /// <inheritdoc/>
        public IDiagnosticsLogger WithValues(LogValueSet valueSet)
        {
            return this;
        }
    }
}
