// <copyright file="JobQueueConsumer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs
{
    /// <summary>
    /// Implements IJobQueueConsumer interface.
    /// </summary>
    public class JobQueueConsumer : DisposableBase, IJobQueueConsumer
    {
        private const string JobQueueConsumerMessage = "job_queue_consumer";

        private readonly BroadcastBlock<IJob> blockJobs = new BroadcastBlock<IJob>(job => job);
        private readonly IDiagnosticsLogger logger;
        private readonly Dictionary<Type, IJobHandler> registeredPayloadTypes = new Dictionary<Type, IJobHandler>();
        private readonly object lockRegisteredPayloadTypes = new object();
        private readonly Dictionary<string, JobHandlerMetrics> jobHandlerMetricsByTypeTag = new Dictionary<string, JobHandlerMetrics>();
        private readonly object lockJobHandlerMetrics = new object();

        // dequeued jobs
        private readonly HashSet<Job> dequeuedJobs = new HashSet<Job>();
        private readonly object lockDequeuedJobs = new object();
        private Task keepInvisibleJobsTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobQueueConsumer"/> class.
        /// </summary>
        /// <param name="queueMessageProducer">A queue producer instance.</param>
        /// <param name="logger">Logger instance.</param>
        public JobQueueConsumer(IQueueMessageProducer queueMessageProducer, IDiagnosticsLogger logger)
        {
            QueueMessageProducer = Requires.NotNull(queueMessageProducer, nameof(queueMessageProducer));
            this.logger = Requires.NotNull(logger, nameof(logger));

            var jobTransformerBlock = new TransformBlock<(QueueMessage, TimeSpan), IJob>((queueMessageInfo) =>
            {
                var payloadTuple = CreateJobPayload(queueMessageInfo.Item1);
                var jobPayloadType = payloadTuple.Item2.GetType();
                var jobType = typeof(Job<>).MakeGenericType(jobPayloadType);
                IJobHandler jobHandler = null;
                lock (this.lockRegisteredPayloadTypes)
                {
                    this.registeredPayloadTypes.TryGetValue(jobPayloadType, out jobHandler);
                }

                var job = (Job)Activator.CreateInstance(jobType, jobHandler, queueMessageProducer.Queue, queueMessageInfo, payloadTuple.Item1, payloadTuple.Item2);
                AddDequeuedJob(job);
                JobCreated?.Invoke(job);
                return job;
            });

            queueMessageProducer.Messages.LinkTo(jobTransformerBlock);
            jobTransformerBlock.LinkTo(this.blockJobs);

            // register default JobPayloadError handler
            var payloadErrorBlock = new ActionBlock<IJob>(
                (job) =>
                {
                    return this.logger.OperationScopeAsync(
                        JobQueueConsumerMessage,
                        (childLogger) =>
                        {
                            childLogger.FluentAddBaseValue(JobQueueLoggerConst.JobId, job.Id)
                                .FluentAddBaseValue(JobQueueLoggerConst.JobType, nameof(JobPayloadError));
                            return CompleteJobAsync((Job)job, JobCompletedStatus.Failed | JobCompletedStatus.Removed | JobCompletedStatus.PayloadError, ((Job<JobPayloadError>)job).Payload.Error);
                        });
                }, JobHandlerBase.DefaultDataflowBlockOptions);
            this.blockJobs.LinkTo(payloadErrorBlock, predicate: (job) => job is IJob<JobPayloadError>);
        }

        /// <inheritdoc/>
        public event Action<IJob> JobCreated;

        /// <summary>
        /// Gets the underlying queue message producer.
        /// </summary>
        public IQueueMessageProducer QueueMessageProducer { get; }

        /// <inheritdoc/>
        public void RegisterJobHandler<T>(IJobHandler<T> jobHandler)
            where T : JobPayload
        {
            // register the payload type on the cache.
            var typeTag = typeof(T).RegisterPayloadType();

            // register on this job consumer
            lock (this.lockRegisteredPayloadTypes)
            {
                this.registeredPayloadTypes.Add(typeof(T), jobHandler);
            }

            ActionBlock<IJob> actionBlock = null;

            // We have to have a wrapper to work with IJob instead of T
            Func<IJob, Task> actionWrapper = async (job) =>
            {
                var jobInstance = (Job)job;
                var jobTyped = (IJob<T>)job;
                this.logger.NewChildLogger().FluentAddValue(JobQueueLoggerConst.JobId, job.Id)
                    .FluentAddValue(JobQueueLoggerConst.JobType, typeTag)
                    .FluentAddValue(JobQueueLoggerConst.JobRetries, jobInstance.JobPayloadInfo.Retries)
                    .LogInfo("job_queue_start_job_handler");
                var jobPayloadOptions = jobInstance.PayloadOptions;

                var jobHandlerOptions = jobInstance.JobHandlerOptions;
                var handlerTimout = jobHandlerOptions != null ? jobHandlerOptions.HandlerTimeout : jobPayloadOptions?.HandlerTimeout;
                var maxHandlerRetries = jobHandlerOptions != null ? jobHandlerOptions.MaxHandlerRetries : jobPayloadOptions?.MaxHandlerRetries;

                var failed = false;
                var retries = 0;
                var cancelled = false;
                var expired = false;

                var start = Stopwatch.StartNew();
                await this.logger.OperationScopeAsync(
                    JobQueueConsumerMessage,
                    async (childLogger) =>
                    {
                        var nowUtc = DateTime.UtcNow;
                        childLogger.FluentAddBaseValue(JobQueueLoggerConst.JobId, job.Id)
                            .FluentAddBaseValue(JobQueueLoggerConst.JobType, typeTag);

                        // pass deserialized logger properties from the producer.
                        foreach (var kvp in jobTyped.Payload.LoggerProperties)
                        {
                            childLogger.FluentAddBaseValue(kvp.Key, kvp.Value.ToString());
                        }

                        if (jobPayloadOptions?.ExpireTimeout.HasValue == true &&
                            nowUtc > job.Created.Add(jobPayloadOptions.ExpireTimeout.Value))
                        {
                            await CompleteJobAsync(jobInstance, JobCompletedStatus.Failed | JobCompletedStatus.Removed | JobCompletedStatus.Expired, null);
                            childLogger.FluentAddValue(JobQueueLoggerConst.JobDidExpired, true);
                            expired = true;
                        }
                        else
                        {
                            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(DisposeToken))
                            {
                                if (handlerTimout.HasValue == true)
                                {
                                    cts.CancelAfter((int)handlerTimout.Value.TotalMilliseconds);
                                }

                                using (cts.Token.Register(() =>
                                    {
                                        cancelled = true;
                                    }))
                                {
                                    await jobHandler.HandleJobAsync(jobTyped, childLogger, cts.Token);
                                    var jobDuration = DateTime.UtcNow - job.Created;
                                    childLogger.FluentAddValue(JobQueueLoggerConst.JobHandlerDuration, start.Elapsed)
                                        .FluentAddValue(JobQueueLoggerConst.JobDuration, jobDuration)
                                        .FluentAddValue(JobQueueLoggerConst.JobQueueDuration, nowUtc - job.Created)
                                        .FluentAddValue(JobQueueLoggerConst.JobDequeuedDuration, nowUtc - jobInstance.DequeueTime);
                                    await CompleteJobAsync(jobInstance, JobCompletedStatus.Succeeded | JobCompletedStatus.Removed, null);
                                }
                            }
                        }
                    },
                    errCallback: async (err, logger) =>
                    {
                        failed = true;
                        logger.FluentAddValue(JobQueueLoggerConst.JobDidCancel, cancelled);

                        var status = JobCompletedStatus.Failed;
                        if (cancelled)
                        {
                            status = status | JobCompletedStatus.Cancelled;
                        }

                        try
                        {
                            ++jobInstance.JobPayloadInfo.Retries;
                            logger.FluentAddValue(JobQueueLoggerConst.JobRetries, jobInstance.JobPayloadInfo.Retries);
                            if (maxHandlerRetries.HasValue == true &&
                                jobInstance.JobPayloadInfo.Retries >= maxHandlerRetries.Value)
                            {
                                status = status | JobCompletedStatus.RetryExhausted | JobCompletedStatus.Removed;
                            }
                            else
                            {
                                ++retries;
                                status = status | JobCompletedStatus.Retry;
                                await jobInstance.UpdateContentAsync(DisposeToken);

                                // if the job handler options has a retry timeout, otherwise it will re appear
                                // on the consumer with the default setting how it was retrieved the first time.
                                if (jobHandlerOptions?.RetryTimeout.HasValue == true)
                                {
                                    await jobInstance.UpdateVisibilityAsync(jobHandlerOptions.RetryTimeout.Value, DisposeToken);
                                }
                            }

                            await CompleteJobAsync(jobInstance, status, err);
                        }
                        catch
                        {
                        }
                    },
                    swallowException: true);

                // update the job handler metrics
                UpdateJobHandlerMetrics(typeTag, (jobHandlerMetrics) =>
                {
                    var inputCount = actionBlock.InputCount;

                    if (inputCount < jobHandlerMetrics.MinInputCount)
                    {
                        jobHandlerMetrics.MinInputCount = inputCount;
                    }

                    if (inputCount > jobHandlerMetrics.MaxInputCount)
                    {
                        jobHandlerMetrics.MaxInputCount = inputCount;
                    }

                    jobHandlerMetrics.AddProcessTime(start.Elapsed);
                    ++jobHandlerMetrics.Processed;
                    jobHandlerMetrics.Retries += retries;
                    if (failed)
                    {
                        ++jobHandlerMetrics.Failures;
                    }

                    if (cancelled)
                    {
                        ++jobHandlerMetrics.Cancelled;
                    }

                    if (expired)
                    {
                        ++jobHandlerMetrics.Expired;
                    }
                });
            };

            // create the action block that executes the handler wrapper
            actionBlock = new ActionBlock<IJob>((job) => actionWrapper(job), jobHandler.DataflowBlockOptions);

            // Link with Predicate - only if a job is of type T
            this.blockJobs.LinkTo(actionBlock, predicate: (job) => job is IJob<T>);
        }

        /// <inheritdoc/>
        public Dictionary<string, IJobHandlerMetrics> GetMetrics()
        {
            lock (this.lockJobHandlerMetrics)
            {
                var results = this.jobHandlerMetricsByTypeTag.ToDictionary(kvp => kvp.Key, kvp => kvp.Value as IJobHandlerMetrics);
                this.jobHandlerMetricsByTypeTag.Clear();
                return results;
            }
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.keepInvisibleJobsTask = KeepInvisibleJobsAsync();
            return QueueMessageProducer.StartAsync(cancellationToken);
        }

        /// <inheritdoc/>
        protected override async Task DisposeInternalAsync()
        {
            await QueueMessageProducer.DisposeAsync();

            if (this.keepInvisibleJobsTask != null)
            {
                try
                {
                    await this.keepInvisibleJobsTask;
                }
                catch (OperationCanceledException)
                {
                }
            }

            await base.DisposeInternalAsync();
        }

        private Task CompleteJobAsync(Job job, JobCompletedStatus status, Exception error)
        {
            RemoveDequeuedJob(job);
            return job.CompleteAsync(status, error, DisposeToken);
        }

        private async Task KeepInvisibleJobsAsync()
        {
            while (!DisposeToken.IsCancellationRequested)
            {
                await Task.Delay(1000, DisposeToken);

                // Note: we assume the jobs won't throw and swallow any error that could end this method.
                var keepInvisibleJobTaskInfoFactories = GetKeepInvisibleJobTaskInfoFactories();

                // update metrics
                foreach (var item in keepInvisibleJobTaskInfoFactories.GroupBy(ti => ti.Item2))
                {
                    UpdateJobHandlerMetrics(item.Key, (jobHandlerMetrics) => jobHandlerMetrics.KeepInvisibleCount += item.Count());
                }

                await Task.WhenAll(keepInvisibleJobTaskInfoFactories.Select(ti => ti.Item1()));
            }
        }

        private void AddDequeuedJob(Job job)
        {
            lock (this.lockDequeuedJobs)
            {
                this.dequeuedJobs.Add(job);
            }
        }

        private void RemoveDequeuedJob(Job job)
        {
            lock (this.lockDequeuedJobs)
            {
                this.dequeuedJobs.Remove(job);
            }
        }

        private (Func<Task>, string)[] GetKeepInvisibleJobTaskInfoFactories()
        {
            lock (this.lockDequeuedJobs)
            {
                var now = DateTime.Now;
                return this.dequeuedJobs.Select(job => job.GetKeepInvisibleTaskInfoFactory(now, this.logger, DisposeToken)).Where(ti => ti.Item1 != null).ToArray();
            }
        }

        private void UpdateJobHandlerMetrics(string typeTag, Action<JobHandlerMetrics> updateCallback)
        {
            lock (this.lockJobHandlerMetrics)
            {
                JobHandlerMetrics jobHandlerMetrics;
                if (!this.jobHandlerMetricsByTypeTag.TryGetValue(typeTag, out jobHandlerMetrics))
                {
                    jobHandlerMetrics = new JobHandlerMetrics();
                    this.jobHandlerMetricsByTypeTag[typeTag] = jobHandlerMetrics;
                }

                updateCallback(jobHandlerMetrics);
            }
        }

        private (JobPayloadInfo, JobPayload) CreateJobPayload(QueueMessage queueMessage)
        {
            try
            {
                var json = Encoding.UTF8.GetString(queueMessage.Content);
                var jobPayloadInfo = JobPayloadInfo.FromJson(json);
                var jobPayload = JobPayloadHelpers.FromJson(jobPayloadInfo.Payload);
                lock (this.lockRegisteredPayloadTypes)
                {
                    if (!this.registeredPayloadTypes.ContainsKey(jobPayload.GetType()))
                    {
                        throw new NotSupportedException($"No registered job handler for type:{jobPayload.GetType()}");
                    }
                }

                return (jobPayloadInfo, jobPayload);
            }
            catch (Exception error)
            {
                this.logger.LogException($"Failed to create payload for job id:{queueMessage.Id}", error);
                return (new JobPayloadInfo(null, DateTime.UtcNow), new JobPayloadError(error));
            }
        }

        private abstract class Job : IJob
        {
            private readonly QueueMessage queueMessage;
            private DateTime invisibleTimeout;
            private int keepInvisibleCount;

            public Job(IQueue queue, (QueueMessage, TimeSpan) messageInfo, JobPayloadInfo jobPayloadInfo)
            {
                Queue = queue;
                this.queueMessage = messageInfo.Item1;
                VisibilityTimeout = messageInfo.Item2;
                JobPayloadInfo = jobPayloadInfo;
                UpdateInvisibleTimeout(DateTime.Now);
            }

            /// <inheritdoc/>
            public event Func<IJobCompleted, CancellationToken, Task> Completed;

            /// <inheritdoc/>
            public string Id => this.queueMessage.Id;

            /// <inheritdoc/>
            public IQueue Queue { get; }

            /// <inheritdoc/>
            public TimeSpan VisibilityTimeout { get; }

            /// <inheritdoc/>
            public DateTime Created => JobPayloadInfo.Created;

            /// <inheritdoc/>
            public int Retries => JobPayloadInfo.Retries;

            public JobPayloadInfo JobPayloadInfo { get; }

            internal abstract JobHandlerOptions JobHandlerOptions { get; }

            internal DateTime DequeueTime { get; } = DateTime.UtcNow;

            internal abstract string TypeTag { get; }

            internal JobPayloadOptions PayloadOptions => JobPayloadInfo.PayloadOptions;

            private TimeSpan InvisibleThreshold
            {
                get
                {
                    return GetValue(PayloadOptions?.InvisibleThreshold, GetValue(JobHandlerOptions?.InvisibleThreshold, TimeSpan.FromSeconds(60)));
                }
            }

            public Task UpdateVisibilityAsync(TimeSpan visibilityTimeout, CancellationToken cancellationToken)
            {
                return Queue.UpdateMessageAsync(this.queueMessage, false, visibilityTimeout, cancellationToken);
            }

            public Task UpdateContentAsync(CancellationToken cancellationToken)
            {
                this.queueMessage.Content = Encoding.UTF8.GetBytes(JobPayloadInfo.ToJson());
                return Queue.UpdateMessageAsync(this.queueMessage, true, VisibilityTimeout, cancellationToken);
            }

            internal async Task CompleteAsync(JobCompletedStatus status, Exception error, CancellationToken cancellationToken)
            {
                if (status.HasFlag(JobCompletedStatus.Removed))
                {
                    await Queue.DeleteMessageAsync(this.queueMessage, cancellationToken);
                }

                if (Completed != null)
                {
                    if (this.keepInvisibleCount > 0)
                    {
                        status |= JobCompletedStatus.KeepInvisible;
                    }

                    var jobCompleted = new JobCompleted()
                    {
                        Status = status,
                        Error = error,
                        KeepInvisibleCount = this.keepInvisibleCount,
                    };
                    foreach (var completed in Completed.GetInvocationList().Cast<Func<IJobCompleted, CancellationToken, Task>>())
                    {
                        await completed(jobCompleted, cancellationToken);
                    }
                }
            }

            internal (Func<Task>, string) GetKeepInvisibleTaskInfoFactory(DateTime now, IDiagnosticsLogger logger, CancellationToken cancellationToken)
            {
                // either we pass the invisible timeout or we are at leaset 1 min closer.
                if (InvisibleThreshold != TimeSpan.Zero && (now >= this.invisibleTimeout || (this.invisibleTimeout - now) < InvisibleThreshold))
                {
                    return (() => logger.OperationScopeAsync(
                        "job_keep_invisible",
                        async (childLogger) =>
                        {
                            ++this.keepInvisibleCount;
                            childLogger.FluentAddValue(JobQueueLoggerConst.JobId, Id)
                                .FluentAddValue(JobQueueLoggerConst.JobType, TypeTag);
                            await UpdateVisibilityAsync(VisibilityTimeout, cancellationToken);
                            UpdateInvisibleTimeout(now);
                        },
                        swallowException: true),
                        TypeTag);
                }

                return (null, null);
            }

            private static TimeSpan GetValue(TimeSpan? value, TimeSpan defaultValue)
            {
                return value.HasValue ? value.Value : defaultValue;
            }

            private void UpdateInvisibleTimeout(DateTime now)
            {
                this.invisibleTimeout = now.Add(VisibilityTimeout);
            }

            private class JobCompleted : IJobCompleted
            {
                public JobCompletedStatus Status { get; set; }

                public Exception Error { get; set; }

                public int KeepInvisibleCount { get; set; }
            }
        }

        private class Job<T> : Job, IJob<T>
            where T : JobPayload
        {
            public Job(IJobHandler<T> jobHandler, IQueue queue, (QueueMessage, TimeSpan) message, JobPayloadInfo jobPayloadInfo, T payload)
                : base(queue, message, jobPayloadInfo)
            {
                Payload = payload;
                JobHandlerOptions = jobHandler?.GetJobOptions(this);
            }

            /// <inheritdoc/>
            public T Payload { get; }

            internal override JobHandlerOptions JobHandlerOptions { get; }

            internal override string TypeTag => JobPayloadHelpers.GetTypeTag(typeof(T));
        }

        private class JobHandlerMetrics : IJobHandlerMetrics
        {
            private readonly List<TimeSpan> jobProcessTimes = new List<TimeSpan>();

            public int MinInputCount { get; set; }

            public int MaxInputCount { get; set; }

            public TimeSpan ProcessTime { get; private set; }

            public int Processed { get; set; }

            public int Failures { get; set; }

            public int Retries { get; set; }

            public int Cancelled { get; set; }

            public int Expired { get; set; }

            public int KeepInvisibleCount { get; set; }

            public IReadOnlyCollection<TimeSpan> ProcessTimes => this.jobProcessTimes;

            public void AddProcessTime(TimeSpan timeSpan)
            {
                ProcessTime += timeSpan;
                this.jobProcessTimes.Add(timeSpan);
            }
        }
    }
}
