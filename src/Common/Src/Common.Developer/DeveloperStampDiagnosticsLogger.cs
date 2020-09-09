// <copyright file="DeveloperStampDiagnosticsLogger.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            this.DiagnosticsLoggers.Add(new JsonStdoutLogger(baseValues, stream));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeveloperStampDiagnosticsLogger"/> class.
        /// </summary>
        /// <param name="baseValues">Base values.</param>
        /// <param name="streams">Text writer streams.</param>
        public DeveloperStampDiagnosticsLogger(LogValueSet baseValues, List<TextWriter> streams)
        {
            foreach (var stream in streams)
            {
                this.DiagnosticsLoggers.Add(new JsonStdoutLogger(baseValues, stream));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeveloperStampDiagnosticsLogger"/> class.
        /// </summary>
        /// <param name="diagnosticsLoggers">Initialized diagnosticsLoggers.</param>
        public DeveloperStampDiagnosticsLogger(IEnumerable<IDiagnosticsLogger> diagnosticsLoggers)
        {
            this.DiagnosticsLoggers = diagnosticsLoggers.ToList();
        }

        private List<IDiagnosticsLogger> DiagnosticsLoggers { get; set; } = new List<IDiagnosticsLogger>();

        /// <inheritdoc/>
        public void AddBaseValue(string key, string value)
        {
            foreach (var jsonStdoutLogger in DiagnosticsLoggers)
            {
                jsonStdoutLogger.AddBaseValue(key, value);
            } 
        }

        /// <inheritdoc/>
        public void AddValue(string key, string value)
        {
            foreach (var jsonStdoutLogger in DiagnosticsLoggers)
            {
                jsonStdoutLogger.AddValue(key, value);
            }
        }

        /// <inheritdoc/>
        public bool Audit(AuditScope scope, string operationName, AuditEventCategory category, CallerIdentity identity, TargetResource targetResource, OperationResult result)
        {
            var auditResult = false;
            foreach (var jsonStdioLogger in this.DiagnosticsLoggers)
            {
                auditResult = jsonStdioLogger.Audit(scope, operationName, category, identity, targetResource, result);
            }

            return auditResult;
        }

        /// <inheritdoc/>
        public bool Audit(AuditScope scope, AuditMandatoryProperties auditMandatoryProperties)
        {
            var auditResult = false;
            foreach (var jsonStdoutLogger in DiagnosticsLoggers)
            {
                auditResult = jsonStdoutLogger.Audit(scope, auditMandatoryProperties);
            }

            return auditResult;
        }

        /// <inheritdoc/>
        public void Log(string message, string level)
        {
            foreach (var jsonStdoutLogger in DiagnosticsLoggers)
            {
                jsonStdoutLogger.Log(message, level);
            }
        }

        /// <inheritdoc/>
        public void LogCritical(string message)
        {
            foreach (var jsonStdoutLogger in DiagnosticsLoggers)
            {
                jsonStdoutLogger.LogCritical(message);
            }
        }

        /// <inheritdoc/>
        public void LogError(string message)
        {
            foreach (var jsonStdoutLogger in DiagnosticsLoggers)
            {
                jsonStdoutLogger.LogError(message);
            }
        }

        /// <inheritdoc/>
        public void LogInfo(string message)
        {
            foreach (var jsonStdoutLogger in DiagnosticsLoggers)
            {
                jsonStdoutLogger.LogInfo(message);
            }
        }

        /// <inheritdoc/>
        public void LogWarning(string message)
        {
            foreach (var jsonStdoutLogger in DiagnosticsLoggers)
            {
                jsonStdoutLogger.LogWarning(message);
            }
        }

        /// <inheritdoc/>
        public IDiagnosticsLogger WithValue(string key, string value)
        {
            return new DeveloperStampDiagnosticsLogger(this.DiagnosticsLoggers.Select(l => l.WithValue(key, value)));
        }

        /// <inheritdoc/>
        public IDiagnosticsLogger WithValues(LogValueSet valueSet)
        {
            return new DeveloperStampDiagnosticsLogger(this.DiagnosticsLoggers.Select(l => l.WithValues(valueSet)));
        }
    }
}
