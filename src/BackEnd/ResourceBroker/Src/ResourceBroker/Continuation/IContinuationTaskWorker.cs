// <copyright file="IContinuationTaskWorker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation
{
    /// <summary>
    /// Continuation worker that gets available messages and passes them off to the activator
    /// for processing.
    /// </summary>
    public interface IContinuationTaskWorker : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether this worker is busy. Used as a proxy to
        /// determine whether it could be cleanned up.
        /// </summary>
        int ActivityLevel { get; }

        /// <summary>
        /// Runs the job that checks to see if there are any messages that need to be
        /// processed.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns if any message was processed.</returns>
        Task<bool> RunAsync(IDiagnosticsLogger logger);
    }
}
