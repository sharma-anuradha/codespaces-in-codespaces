// <copyright file="NullDiagnosticsLoggerFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil
{
    /// <inheritdoc/>
    public class NullDiagnosticsLoggerFactory : IDiagnosticsLoggerFactory
    {
        /// <inheritdoc/>
        public IDiagnosticsLogger New(LogValueSet logValueSet = null)
        {
            return new NullLogger();
        }
    }
}
