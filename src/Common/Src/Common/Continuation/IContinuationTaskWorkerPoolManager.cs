// <copyright file="IContinuationTaskWorkerPoolManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation
{
    /// <summary>
    /// Continuation manager that controls the workers that gets available messages
    /// and passes them off to the activator for processing.
    /// </summary>
    public interface IContinuationTaskWorkerPoolManager : IDisposable
    {
        /// <summary>
        /// Gets the total count of how many workers are in play.
        /// </summary>
        int CurrentWorkerCount { get; }

        /// <summary>
        /// Kicks off the manager which tracks the workers.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Task for the operation.</returns>
        Task StartAsync(IDiagnosticsLogger logger);
    }
}
