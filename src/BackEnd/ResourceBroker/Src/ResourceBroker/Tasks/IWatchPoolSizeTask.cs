// <copyright file="IWatchPoolSizeTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    public interface IWatchPoolSizeTask : IDisposable
    {
        /// <summary>
        /// This job, for each resource sku we have, will move through those resources randomly
        /// and in parallel. As each resource is processed, it will attempt to obtain a lock on
        /// that resource, if it can't obtain a lock, it will continue onto the next item (as its
        /// assumed another worker is successfully working on that data), if it can obtain a lock,
        /// it will determine how many items need to be added to the job queue and add those items
        /// to the queue.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task<bool> RunAsync(IDiagnosticsLogger rootLogger);
    }
}
