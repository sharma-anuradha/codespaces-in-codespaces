// <copyright file="JobSchedulerProducerHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// Helper extension for the IJobScheduler interface.
    /// </summary>
    public static class JobSchedulerProducerHelpers
    {
        /// <summary>
        /// Create a job scheduler payload factory based on a fucntion callback.
        /// </summary>
        /// <param name="jobPayloadInfoFactory">The callback to invoke to produce payloads.</param>
        /// <returns>Instance of a IJobSchedulePayloadFactory interface.</returns>
        public static IJobSchedulePayloadFactory CreateJobSchedulePayloadFactory(
            Func<string, DateTime, IServiceProvider, IDiagnosticsLogger, CancellationToken, Task<IEnumerable<(JobPayload, JobPayloadOptions)>>> jobPayloadInfoFactory)
        {
            return new JobSchedulePayloadFactory(jobPayloadInfoFactory);
        }

        /// <summary>
        /// Add a delayed job that will produce a job payload.
        /// </summary>
        /// <param name="jobScheduler">The job scheduler instance.</param>
        /// <param name="delay">Job run delay..</param>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="jobQueueProducer">The job queue producer instance.</param>
        /// <param name="jobSchedulePayloadFactory">A job schedule payload factory instance.</param>
        /// <returns>A schedule job.</returns>
        public static IScheduleJob AddDelayedJobPayload(
            this IJobScheduler jobScheduler,
            TimeSpan delay,
            string jobName,
            IJobQueueProducer jobQueueProducer,
            IJobSchedulePayloadFactory jobSchedulePayloadFactory)
        {
            Requires.NotNull(jobScheduler, nameof(jobScheduler));
            Requires.NotNull(jobQueueProducer, nameof(jobQueueProducer));
            Requires.NotNull(jobSchedulePayloadFactory, nameof(jobSchedulePayloadFactory));

            return jobScheduler.AddDelayedJob(delay, jobName, async (jobRunId, dt, srvc, logger, ct) =>
            {
                var jobPayloadInfos = await jobSchedulePayloadFactory.CreatePayloadsAsync(jobRunId, dt, srvc, logger, ct);
                await AddJobsAsync(jobQueueProducer, jobPayloadInfos, jobRunId, dt, logger, ct);
            });
        }

        /// <summary>
        /// Add a delayed job that will produce a job payload.
        /// </summary>
        /// <param name="jobScheduler">The job scheduler instance.</param>
        /// <param name="delay">Job run delay..</param>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="jobQueueProducer">The job queue producer instance.</param>
        /// <param name="jobPayloadInfoFactory">A job payload info factory.</param>
        /// <returns>A schedule job.</returns>
        public static IScheduleJob AddDelayedJobPayload(
            this IJobScheduler jobScheduler,
            TimeSpan delay,
            string jobName,
            IJobQueueProducer jobQueueProducer,
            Func<string, DateTime, IServiceProvider, IDiagnosticsLogger, CancellationToken, Task<IEnumerable<(JobPayload, JobPayloadOptions)>>> jobPayloadInfoFactory)
        {
            return AddDelayedJobPayload(jobScheduler, delay, jobName, jobQueueProducer, new JobSchedulePayloadFactory(jobPayloadInfoFactory));
        }

        /// <summary>
        /// Add a delayed job that will produce a job payload.
        /// </summary>
        /// <param name="jobScheduler">The job scheduler instance.</param>
        /// <param name="delay">Job run delay..</param>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="jobQueueProducer">The job queue producer instance.</param>
        /// <param name="jobPayload">The job payload instance.</param>
        /// <param name="jobPayloadOptions">The optional job payload options.</param>
        /// <returns>A schedule job.</returns>
        public static IScheduleJob AddDelayedJobPayload(
            this IJobScheduler jobScheduler,
            TimeSpan delay,
            string jobName,
            IJobQueueProducer jobQueueProducer,
            JobPayload jobPayload,
            JobPayloadOptions jobPayloadOptions = null)
        {
            return jobScheduler.AddDelayedJobPayload(delay, jobName, jobQueueProducer, (jobRunId, dt, srvcProvider, logger, ct) => Task.FromResult(Enumerable.Repeat((jobPayload, jobPayloadOptions), 1)));
        }

        /// <summary>
        /// Add a delayed job that will produce a job payload.
        /// </summary>
        /// <typeparam name="T">Tag type to use for the payload.</typeparam>
        /// <param name="jobScheduler">The job scheduler instance.</param>
        /// <param name="delay">Job run delay..</param>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="jobQueueProducer">The job queue producer instance.</param>
        /// <param name="jobPayloadOptions">The optional job payload options.</param>
        /// <returns>A schedule job.</returns>
        public static IScheduleJob AddDelayedJobPayload<T>(
            this IJobScheduler jobScheduler,
            TimeSpan delay,
            string jobName,
            IJobQueueProducer jobQueueProducer,
            JobPayloadOptions jobPayloadOptions = null)
            where T : class
        {
            return jobScheduler.AddDelayedJobPayload(delay, jobName, jobQueueProducer, new JobPayload<T>(), jobPayloadOptions);
        }

        /// <summary>
        /// Add a recurring job that will produce a job payload.
        /// </summary>
        /// <param name="jobScheduler">The job scheduler instance.</param>
        /// <param name="expression">Cron type expression.</param>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="jobQueueProducer">The job queue producer instance.</param>
        /// <param name="jobSchedulePayloadFactory">A job schedule payload factory instance.</param>
        /// <returns>A schedule job.</returns>
        public static IScheduleJob AddRecurringJobPayload(
            this IJobScheduler jobScheduler,
            string expression,
            string jobName,
            IJobQueueProducer jobQueueProducer,
            IJobSchedulePayloadFactory jobSchedulePayloadFactory)
        {
            Requires.NotNull(jobScheduler, nameof(jobScheduler));
            Requires.NotNull(jobQueueProducer, nameof(jobQueueProducer));
            Requires.NotNull(jobSchedulePayloadFactory, nameof(jobSchedulePayloadFactory));

            return jobScheduler.AddRecurringJob(expression, jobName, async (jobRunId, dt, srvc, logger, ct) =>
            {
                var jobPayloadInfos = await jobSchedulePayloadFactory.CreatePayloadsAsync(jobRunId, dt, srvc, logger, ct);
                await AddJobsAsync(jobQueueProducer, jobPayloadInfos, jobRunId, dt, logger, ct);
            });
        }

        /// <summary>
        /// Add a recurring job that will produce a job payload.
        /// </summary>
        /// <param name="jobScheduler">The job scheduler instance.</param>
        /// <param name="expression">Cron type expression.</param>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="jobQueueProducer">The job queue producer instance.</param>
        /// <param name="jobPayloadInfoFactory">A job payload info factory.</param>
        /// <returns>A schedule job.</returns>
        public static IScheduleJob AddRecurringJobPayload(
            this IJobScheduler jobScheduler,
            string expression,
            string jobName,
            IJobQueueProducer jobQueueProducer,
            Func<string, DateTime, IServiceProvider, IDiagnosticsLogger, CancellationToken, Task<IEnumerable<(JobPayload, JobPayloadOptions)>>> jobPayloadInfoFactory)
        {
            return AddRecurringJobPayload(jobScheduler, expression, jobName, jobQueueProducer, new JobSchedulePayloadFactory(jobPayloadInfoFactory));
        }

        /// <summary>
        /// Add a recurring job that will produce a job payload.
        /// </summary>
        /// <param name="jobScheduler">The job scheduler instance.</param>
        /// <param name="expression">Cron type expression.</param>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="jobQueueProducer">The job queue producer instance.</param>
        /// <param name="jobPayload">The job payload instance.</param>
        /// <param name="jobPayloadOptions">The optional job payload options.</param>
        /// <returns>A schedule job.</returns>
        public static IScheduleJob AddRecurringJobPayload(
            this IJobScheduler jobScheduler,
            string expression,
            string jobName,
            IJobQueueProducer jobQueueProducer,
            JobPayload jobPayload,
            JobPayloadOptions jobPayloadOptions = null)
        {
            return jobScheduler.AddRecurringJobPayload(expression, jobName, jobQueueProducer, (jobRunId, dt, srvcProvider, logger, ct) => Task.FromResult(Enumerable.Repeat((jobPayload, jobPayloadOptions), 1)));
        }

        /// <summary>
        /// Add a recurring job that will produce a job payload.
        /// </summary>
        /// <typeparam name="T">Tag type to use for the payload.</typeparam>
        /// <param name="jobScheduler">The job scheduler instance.</param>
        /// <param name="expression">Cron type expression.</param>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="jobQueueProducer">The job queue producer instance.</param>
        /// <param name="jobPayloadOptions">The optional job payload options.</param>
        /// <returns>A schedule job.</returns>
        public static IScheduleJob AddRecurringJobPayload<T>(
            this IJobScheduler jobScheduler,
            string expression,
            string jobName,
            IJobQueueProducer jobQueueProducer,
            JobPayloadOptions jobPayloadOptions = null)
            where T : class
        {
            return jobScheduler.AddRecurringJobPayload(expression, jobName, jobQueueProducer, new JobPayload<T>(), jobPayloadOptions);
        }

        private static async Task AddJobsAsync(
            IJobQueueProducer jobQueueProducer,
            IEnumerable<(JobPayload, JobPayloadOptions)> jobPayloadInfos,
            string jobRunId,
            DateTime jobScheduleRun,
            IDiagnosticsLogger logger,
            CancellationToken cancellationToken)
        {
            foreach (var jobPayloadInfo in jobPayloadInfos)
            {
                jobPayloadInfo.Item1.LoggerProperties.Add("JobRunId", jobRunId);
                jobPayloadInfo.Item1.LoggerProperties.Add("JobScheduleRun", jobScheduleRun);

                await jobQueueProducer.AddJobAsync(jobPayloadInfo.Item1, jobPayloadInfo.Item2, logger, cancellationToken);
            }
        }

        private class JobSchedulePayloadFactory : IJobSchedulePayloadFactory
        {
            private readonly Func<string, DateTime, IServiceProvider, IDiagnosticsLogger, CancellationToken, Task<IEnumerable<(JobPayload, JobPayloadOptions)>>> callback;

            public JobSchedulePayloadFactory(Func<string, DateTime, IServiceProvider, IDiagnosticsLogger, CancellationToken, Task<IEnumerable<(JobPayload, JobPayloadOptions)>>> callback)
            {
                this.callback = callback;
            }

            public Task<IEnumerable<(JobPayload, JobPayloadOptions)>> CreatePayloadsAsync(string jobRunId, DateTime scheduleRun, IServiceProvider serviceProvider, IDiagnosticsLogger logger, CancellationToken cancellationToken)
            {
                return this.callback(jobRunId, scheduleRun, serviceProvider, logger, cancellationToken);
            }
        }
    }
}
