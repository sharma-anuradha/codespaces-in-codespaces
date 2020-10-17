// <copyright file="JobSchedulerProducerHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    public delegate Task JobPayloadInfoFactoryDelegate(string jobRunId, DateTime dt, IServiceProvider provider, OnPayloadsCreatedDelegateAsync onCreated, IDiagnosticsLogger logger, CancellationToken cancellationToken);

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
           JobPayloadInfoFactoryDelegate jobPayloadInfoFactory)
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

            return jobScheduler.AddDelayedJob(delay, jobName, BuildPayloadFactoryScheduleDelegate(jobQueueProducer, jobSchedulePayloadFactory));
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
            JobPayloadInfoFactoryDelegate jobPayloadInfoFactory)
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
            return jobScheduler.AddDelayedJobPayload(delay, jobName, jobQueueProducer, (jobRunId, dt, srvcProvider, onCreated, logger, ct) => onCreated(new[] { (jobPayload, jobPayloadOptions) }, ct));
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

            return jobScheduler.AddRecurringJob(expression, jobName, BuildPayloadFactoryScheduleDelegate(jobQueueProducer, jobSchedulePayloadFactory));
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
            JobPayloadInfoFactoryDelegate jobPayloadInfoFactory)
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
            return jobScheduler.AddRecurringJobPayload(expression, jobName, jobQueueProducer, (jobRunId, dt, srvcProvider, onCreated, logger, ct) => onCreated(new[] { (jobPayload, jobPayloadOptions) }, ct));
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

        private static RunScheduleJobDelegate BuildPayloadFactoryScheduleDelegate(
            IJobQueueProducer jobQueueProducer,
            IJobSchedulePayloadFactory jobSchedulePayloadFactory)
        {
            return async (jobRunId, dt, srvc, logger, ct) =>
            {
                Task OnPayloadsCreatedAsync(IEnumerable<(JobPayload payload, JobPayloadOptions options)> jobPayloads, CancellationToken cancellationToken)
                {
                    foreach (var jobPayload in jobPayloads)
                    {
                        jobPayload.payload.LoggerProperties.Add("JobRunId", jobRunId);
                        jobPayload.payload.LoggerProperties.Add("JobScheduleRun", dt);
                    }

                    return jobQueueProducer.AddJobsAsync(jobPayloads, logger, cancellationToken);
                }

                await jobSchedulePayloadFactory.CreatePayloadsAsync(jobRunId, dt, srvc, OnPayloadsCreatedAsync, logger, ct);
            };
        }

        private class JobSchedulePayloadFactory : IJobSchedulePayloadFactory
        {
            private readonly JobPayloadInfoFactoryDelegate callback;

            public JobSchedulePayloadFactory(JobPayloadInfoFactoryDelegate callback)
            {
                this.callback = callback;
            }

            public Task CreatePayloadsAsync(string jobRunId, DateTime scheduleRun, IServiceProvider serviceProvider, OnPayloadsCreatedDelegateAsync onCreated, IDiagnosticsLogger logger, CancellationToken cancellationToken)
            {
                return this.callback(jobRunId, scheduleRun, serviceProvider, onCreated, logger, cancellationToken);
            }
        }
    }
}
