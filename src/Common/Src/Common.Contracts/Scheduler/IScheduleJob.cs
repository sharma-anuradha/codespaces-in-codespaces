// <copyright file="IScheduleJob.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts
{
    /// <summary>
    /// A schedule job contract.
    /// </summary>
    public interface IScheduleJob : IDisposable
    {
        /// <summary>
        /// Job started event.
        /// </summary>
        event Func<IJobStartInfo, Task> JobStart;

        /// <summary>
        /// Job ended event.
        /// </summary>
        event Func<IJobEndInfo, Task> JobEnd;

        /// <summary>
        /// Gets the next date/time this job would run.
        /// </summary>
        DateTime NextRun { get; }

        /// <summary>
        /// Run this job now.
        /// </summary>
        /// <param name="stoppingToken">Stopping token.</param>
        /// <returns>Completion task.</returns>
        Task RunNowAsync(CancellationToken stoppingToken);
    }
}
