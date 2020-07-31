// <copyright file="JobSchedulerHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts
{
    /// <summary>
    /// Helper extension for the IJobScheduler interface.
    /// </summary>
    public static class JobSchedulerHelpers
    {
        /// <summary>
        /// Add a recurring job using a callback.
        /// </summary>
        /// <param name="jobScheduler">The job scheduler instance.</param>
        /// <param name="expression">Cron type expression.</param>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="runScheduleJobCallback">The callback when the job is run.</param>
        /// <returns>A schedule job.</returns>
        public static IScheduleJob AddRecurringJob(this IJobScheduler jobScheduler, string expression, string jobName, Func<string, DateTime, IServiceProvider, IDiagnosticsLogger, CancellationToken, Task> runScheduleJobCallback)
        {
            Requires.NotNull(jobScheduler, nameof(jobScheduler));
            Requires.NotNull(runScheduleJobCallback, nameof(runScheduleJobCallback));
            return jobScheduler.AddRecurringJob(expression, new RunScheduleJob(jobName, runScheduleJobCallback));
        }

        /// <summary>
        /// Add a delayed job using a callback.
        /// </summary>
        /// <param name="jobScheduler">The job scheduler instance.</param>
        /// <param name="delay">Delay amount for the job to run.</param>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="runScheduleJobCallback">The callback when the job is run.</param>
        /// <returns>A schedule job.</returns>
        public static IScheduleJob AddDelayedJob(this IJobScheduler jobScheduler, TimeSpan delay, string jobName, Func<string, DateTime, IServiceProvider, IDiagnosticsLogger, CancellationToken, Task> runScheduleJobCallback)
        {
            Requires.NotNull(jobScheduler, nameof(jobScheduler));
            Requires.NotNull(runScheduleJobCallback, nameof(runScheduleJobCallback));
            return jobScheduler.AddDelayedJob(delay, new RunScheduleJob(jobName, runScheduleJobCallback));
        }

        private class RunScheduleJob : IRunScheduleJob
        {
            private readonly Func<string, DateTime, IServiceProvider, IDiagnosticsLogger, CancellationToken, Task> runScheduleJob;

            public RunScheduleJob(string jobName, Func<string, DateTime, IServiceProvider, IDiagnosticsLogger, CancellationToken, Task> runScheduleJob)
            {
                Name = jobName;
                this.runScheduleJob = runScheduleJob;
            }

            public string Name { get; }

            public Task RunAsync(string jobRunId, DateTime scheduleRun, IServiceProvider serviceProvider, IDiagnosticsLogger logger, CancellationToken cancellationToken)
            {
                return this.runScheduleJob(jobRunId, scheduleRun, serviceProvider, logger, cancellationToken);
            }
        }
    }
}
