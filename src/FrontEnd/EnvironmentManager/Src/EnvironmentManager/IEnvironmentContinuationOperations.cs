// <copyright file="IEnvironmentContinuationOperations.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Resource continuation operations to make involving/starting specific handlers
    /// easier.
    /// </summary>
    public interface IEnvironmentContinuationOperations
    {
        /// <summary>
        /// Create compute resource by invoking the continution activator.
        /// </summary>
        /// <param name="environmentId">Target resource id.</param>
        /// <param name="lastStateUpdated">Target last state updated.</param>
        /// <param name="reason">Trigger for operation.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Resuling continuation result.</returns>
        Task<ContinuationResult> ArchiveAsync(
            Guid environmentId,
            DateTime lastStateUpdated,
            string reason,
            IDiagnosticsLogger logger);
    }
}
