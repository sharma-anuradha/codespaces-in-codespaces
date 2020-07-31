// <copyright file="JobScheduler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts;
using NCrontab;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler
{
    /// <summary>
    /// The Job scheduler implementation.
    /// </summary>
    public class JobScheduler : BackgroundService, IJobScheduler
    {
        private readonly List<ScheduleJob> scheduleJobs = new List<ScheduleJob>();
        private readonly object lockScheduleJobs = new object();

        private CancellationTokenSource updateCts = new CancellationTokenSource();

        /// <summary>
        /// Initializes a new instance of the <see cref="JobScheduler"/> class.
        /// </summary>
        /// <param name="serviceScopeFactory">A service scope factory instance.</param>
        /// <param name="logger">The logger instance.</param>
        public JobScheduler(IServiceScopeFactory serviceScopeFactory, IDiagnosticsLogger logger)
        {
            ServiceScopeFactory = Requires.NotNull(serviceScopeFactory, nameof(serviceScopeFactory));
            Logger = Requires.NotNull(logger, nameof(logger));
        }

        private IServiceScopeFactory ServiceScopeFactory { get; }

        private IDiagnosticsLogger Logger { get; }

        /// <inheritdoc/>
        public IScheduleJob AddRecurringJob(string expression, IRunScheduleJob runScheduleJob)
        {
            bool includingSeconds = expression.Split(' ').Length == 6;
            var crontab = CrontabSchedule.Parse(expression, new CrontabSchedule.ParseOptions() { IncludingSeconds = includingSeconds });

            var recurringJob = new RecurringJob(this, crontab, runScheduleJob);
            AddScheduleJob(recurringJob);
            return recurringJob;
        }

        /// <inheritdoc/>
        public IScheduleJob AddDelayedJob(TimeSpan delay, IRunScheduleJob runScheduleJob)
        {
            var delayedJob = new DelayedJob(this, DateTime.UtcNow.Add(delay), runScheduleJob);
            AddScheduleJob(delayedJob);
            return delayedJob;
        }

        /// <inheritdoc/>
        public IEnumerable<IScheduleJob> GetScheduleJobs(string jobName)
        {
            lock (this.lockScheduleJobs)
            {
                return this.scheduleJobs.Where(j => j.Name == jobName).ToArray();
            }
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                TimeSpan timeSpan = TimeSpan.Zero;
                lock (this.lockScheduleJobs)
                {
                    var scheduleJob = this.scheduleJobs.FirstOrDefault();
                    if (scheduleJob != null)
                    {
                        var nextRun = scheduleJob.NextRun;
                        if (now >= nextRun)
                        {
                            this.scheduleJobs.RemoveAt(0);
                            if (scheduleJob.UpdateNextRun(now.AddMilliseconds(1)))
                            {
                                // Note: this will put the recurring in the proper order.
                                InsertScheduleJob(scheduleJob);
                            }

                            // Run this job.
                            scheduleJob.RunNext(nextRun, stoppingToken);
                        }
                        else
                        {
                            // Calculate the amount of time to wait for the next job to run.
                            timeSpan = scheduleJob.NextRun - now;
                        }
                    }
                    else
                    {
                        timeSpan = TimeSpan.FromMinutes(1);
                    }
                }

                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, updateCts.Token))
                {
                    try
                    {
                        await Task.Delay(timeSpan, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // this cancellation exception could be caused by the stopping token or
                        // forced by the additon/removel of schedule jobs.
                    }
                }
            }
        }

        /// <summary>
        /// This will enforce our Task.Delay to be cancelled to allow the schedule jobs to re-evaluate
        /// the next waiting time.
        /// </summary>
        private void ForceUpdate()
        {
            var updateCts = this.updateCts;
            this.updateCts = new CancellationTokenSource();
            updateCts.Cancel();
        }

        private void AddScheduleJob(ScheduleJob scheduleJob)
        {
            lock (this.lockScheduleJobs)
            {
                InsertScheduleJob(scheduleJob);
            }

            ForceUpdate();
        }

        private void RemoveScheduleJob(ScheduleJob scheduleJob)
        {
            lock (this.lockScheduleJobs)
            {
                if (this.scheduleJobs.Remove(scheduleJob))
                {
                    ForceUpdate();
                }
            }
        }

        /// <summary>
        /// Insert a new schedule job in order.
        /// </summary>
        /// <param name="scheduleJob">The schedule job instance.</param>
        private void InsertScheduleJob(ScheduleJob scheduleJob)
        {
            var index = this.scheduleJobs.BinarySearch(scheduleJob, JobRunComparer.Instance);
            if (index < 0)
            {
                index = ~index;
            }

            this.scheduleJobs.Insert(index, scheduleJob);
        }

        /// <summary>
        /// Helper class to compare 2 schedule job instances.
        /// </summary>
        private class JobRunComparer : IComparer<ScheduleJob>
        {
            public static readonly JobRunComparer Instance = new JobRunComparer();

            public int Compare([AllowNull] ScheduleJob x, [AllowNull] ScheduleJob y)
            {
                return DateTime.Compare(x.NextRun, y.NextRun);
            }
        }

        /// <summary>
        /// Base class for a shedule job.
        /// </summary>
        private abstract class ScheduleJob : IScheduleJob
        {
            private readonly IRunScheduleJob runScheduleJob;

            protected ScheduleJob(JobScheduler jobScheduler, IRunScheduleJob runScheduleJob)
            {
                JobScheduler = jobScheduler;
                this.runScheduleJob = runScheduleJob;
            }

            public event Func<IJobStartInfo, Task> JobStart;

            public event Func<IJobEndInfo, Task> JobEnd;

            public abstract DateTime NextRun { get; }

            internal string Name => this.runScheduleJob.Name;

            protected abstract DateTime? NextJobRun { get; }

            private JobScheduler JobScheduler { get; }

            private IDiagnosticsLogger Logger => JobScheduler.Logger;

            public void Dispose()
            {
                JobScheduler.RemoveScheduleJob(this);
            }

            public abstract bool UpdateNextRun(DateTime now);

            public Task RunNowAsync(CancellationToken stoppingToken)
            {
                return RunNextAsync(DateTime.UtcNow, stoppingToken);
            }

            internal void RunNext(DateTime scheduleRun, CancellationToken stoppingToken)
            {
                Task.Run(() => RunNextAsync(scheduleRun, stoppingToken));
            }

            private Task RunNextAsync(DateTime scheduleRun, CancellationToken stoppingToken)
            {
                var startTime = DateTime.UtcNow;
                var start = Stopwatch.StartNew();
                Func<Exception, Task> jobFireEnd = (e) =>
                {
                    return JobEnd?.Invoke(new JobEndInfo(startTime, start.Elapsed, NextJobRun, e)) ?? Task.CompletedTask;
                };

                return Logger.OperationScopeAsync(
                    "job_scheduler_run",
                    async (childLogger) =>
                    {
                        var jobRunId = Guid.NewGuid().ToString();

                        childLogger.FluentAddValue(JobSchedulerLoggerConst.JobName, this.runScheduleJob.Name)
                            .FluentAddValue(JobSchedulerLoggerConst.JobScheduleRun, scheduleRun)
                            .FluentAddValue(JobSchedulerLoggerConst.JobRunId, jobRunId);

                        using (var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken))
                        {
                            await (JobStart?.Invoke(new JobStartInfo(startTime, cts)) ?? Task.CompletedTask);
                            using (var scope = JobScheduler.ServiceScopeFactory.CreateScope())
                            {
                                await this.runScheduleJob.RunAsync(jobRunId, scheduleRun, scope.ServiceProvider, childLogger, cts.Token);
                                await jobFireEnd(null);
                            }
                        }
                    },
                    errCallback: (err, logger) =>
                    {
                        return jobFireEnd(err);
                    });
            }

            /// <summary>
            /// Implements IJobStartInfo.
            /// </summary>
            private class JobStartInfo : IJobStartInfo
            {
                private readonly CancellationTokenSource cancellationTokenSource;

                public JobStartInfo(DateTime startTime, CancellationTokenSource cancellationTokenSource)
                {
                    StartTime = startTime;
                    this.cancellationTokenSource = cancellationTokenSource;
                }

                public DateTime StartTime { get; }

                public void Cancel()
                {
                    this.cancellationTokenSource.Cancel();
                }
            }

            /// <summary>
            /// Implements IJobEndInfo.
            /// </summary>
            private class JobEndInfo : IJobEndInfo
            {
                public JobEndInfo(DateTime startTime, TimeSpan duration, DateTime? nextRun, Exception exception = null)
                {
                    StartTime = startTime;
                    Duration = duration;
                    NextRun = nextRun;
                    Exception = exception;
                }

                public DateTime StartTime { get; }

                public TimeSpan Duration { get; }

                public Exception Exception { get; }

                public DateTime? NextRun { get; }
            }
        }

        /// <summary>
        /// Our recurring job implementation.
        /// </summary>
        private class RecurringJob : ScheduleJob
        {
            private readonly CrontabSchedule crontab;
            private DateTime nextRun;

            public RecurringJob(JobScheduler jobScheduler, CrontabSchedule crontab, IRunScheduleJob runScheduleJob)
                : base(jobScheduler, runScheduleJob)
            {
                this.crontab = crontab;
                UpdateNextRun(DateTime.UtcNow);
            }

            public override DateTime NextRun => this.nextRun;

            protected override DateTime? NextJobRun => NextRun;

            public override bool UpdateNextRun(DateTime now)
            {
                this.nextRun = this.crontab.GetNextOccurrence(now);
                return true;
            }
        }

        /// <summary>
        /// Our delayed job implementation.
        /// </summary>
        private class DelayedJob : ScheduleJob
        {
            public DelayedJob(JobScheduler jobScheduler, DateTime nextRun, IRunScheduleJob runScheduleJob)
                : base(jobScheduler, runScheduleJob)
            {
                NextRun = nextRun;
            }

            public override DateTime NextRun { get; }

            protected override DateTime? NextJobRun => null;

            public override bool UpdateNextRun(DateTime now) => false;
        }
    }
}
