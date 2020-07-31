// <copyright file="IJobScheduler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts
{
    /// <summary>
    /// The Job scheduler definition.
    /// </summary>
    public interface IJobScheduler
    {
        /// <summary>
        /// Add a recurring job to be executed by the scheduler.
        /// </summary>
        /// <param name="expression">A cron expression to define the recurring date/time.</param>
        /// <param name="runScheduleJob">The run schedule job instance.</param>
        /// <returns>A schedule job instance.</returns>
        IScheduleJob AddRecurringJob(string expression, IRunScheduleJob runScheduleJob);

        /// <summary>
        /// Add a delayed job to be executed by the job scheduler.
        /// </summary>
        /// <param name="delay">Time to delay teh run. </param>
        /// <param name="runScheduleJob">The run schedule job instance.</param>
        /// <returns>A schedule job instance.</returns>
        IScheduleJob AddDelayedJob(TimeSpan delay, IRunScheduleJob runScheduleJob);

        /// <summary>
        /// Get matching job names that are being scheduled.
        /// </summary>
        /// <param name="jobName">Namf of the job to match.</param>
        /// <returns>List of mathcing jobs.</returns>
        IEnumerable<IScheduleJob> GetScheduleJobs(string jobName);
    }
}
