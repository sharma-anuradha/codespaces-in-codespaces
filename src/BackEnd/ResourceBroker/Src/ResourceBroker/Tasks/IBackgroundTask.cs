// <copyright file="IBackgroundTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Defines a task which is designed to run periodically in the background.
    /// </summary>
    public interface IBackgroundTask : IDisposable
    {
        /// <summary>
        /// Core task which is executed.
        /// </summary>
        /// <param name="taskInterval">The interval (frequency) at which the task should be executed.</param>
        /// <param name="logger">The logger to use during task execution.</param>
        /// <returns>Whether the task should run again.</returns>
        /// <remarks>The <paramref name="taskInterval"/> is used to determine whether or not the task has real work to do.
        /// It's the responsibility of the task to retrieve a <see cref="ClaimedDistributedLease"/> and verify whether
        /// it should execute.</remarks>
        Task<bool> RunAsync(TimeSpan taskInterval, IDiagnosticsLogger logger);
    }
}
