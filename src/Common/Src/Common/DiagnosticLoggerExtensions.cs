// <copyright file="DiagnosticLoggerExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Extension methods for <see cref="IDiagnosticsLogger"/>.
    /// </summary>
    public static class DiagnosticLoggerExtensions
    {
        /// <summary>
        /// Adds documentId to the logger.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="documentId">The documentId.</param>
        /// <returns>The logger with additional values.</returns>
        public static IDiagnosticsLogger AddDocumentId(this IDiagnosticsLogger logger, string documentId)
        {
            return logger.FluentAddValue("DocumentId", documentId);
        }
    }
}
