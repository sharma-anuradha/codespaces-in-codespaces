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

        /// <summary>
        /// Default max number of retries
        /// </summary>
        private const int DefaultMaxRetries = 5;

        private readonly IDiagnosticsLogger logger;
        private readonly Dictionary<Type, JobHandlerInfo> registeredJobHandlers = new Dictionary<Type, JobHandlerInfo>();
        private readonly object lockRegisteredJobHandlers = new object();
        private readonly Dictionary<string, JobHandlerMetrics> jobHandlerMetricsByTypeTag = new Dictionary<string, JobHandlerMetrics>();
        private readonly object lockJobHandlerMetrics = new object();

        private static Lazy<string> payloadErrorTypeTag = new Lazy<string>(() => typeof(JobPayloadError).RegisterPayloadType());

        // dequeued jobs
        private readonly HashSet<Job> dequeuedJobs = new HashSet<Job>();
        private readonly object lockDequeuedJobs = new object();
        private Task keepInvisibleJobsTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobQueueConsumer"/> class.
        /// </summary>
        /// <param name="queue">A queue  instance.</param>
        /// <param name="logger">Logger instance.</param>
        public JobQueueConsumer(IQueue queue, IDiagnosticsLogger logger)
        {
            Queue = Requires.NotNull(queue, nameof(queue));
            this.logger = Requires.NotNull(logger, nameof(logger));
        }

        /// <inheritdoc/>
        public event Action<IJob> JobCreated;

        private IQueue Queue { get; }

        private bool IsRunning => this.keepInvisibleJobsTask != null;

        /// <inheritdoc/>
        public void RegisterJobHandler<T>(IJobHandler<T> jobHandler)
            where T : JobPayload
        {
            // register the job handler info
            lock (this.lockRegisteredJobHandlers)
            {
                if (this.registeredJobHandlers.ContainsKey(typeof(T)))
                {
                    throw new InvalidOperationException($"type:'{typeof(T).Name}' already registered");
                }

                var jobHandlerInfo = new JobHandlerInfo(jobHandler, CreateJobHandlerActionBlock<T>(jobHandler));
                this.registeredJobHandlers.Add(typeof(T), jobHandlerInfo);
            }

            // in case the job handler want to be notified when registration has completed.
            if (jobHandler is IJobHandlerRegisterCallback jobHandlerRegisterCallback)
            {
                jobHandlerRegisterCallback.OnRegisterJobHandler(this);
            }
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
        public Task StartAsync(QueueMessageProducerSettings queueMessageProducerSettings, CancellationToken cancellationToken)
        {
            queueMessageProducerSettings = queueMessageProducerSettings ?? QueueMessageProducerSettings.Default;

            ThrowIfRunning();

            // register default JobPayloadError handler
            var payloadErrorHandler = JobQueueConsumerHelpers.CreateJobHandler<JobPayloadError>(
                (job, logger, ct) =>
                {
                    return logger.OperationScopeAsync(
                        JobQueueConsumerMessage,
                        (childLogger) =>
                        {
                            childLogger.FluentAddBaseValue(JobQueueLoggerConst.JobId, job.Id)
                                .FluentAddBaseValue(JobQueueLoggerConst.JobType, nameof(JobPayloadError));
                            return CompleteJobAsync((Job)job, JobCompletedStatus.Failed | JobCompletedStatus.Removed | JobCompletedStatus.PayloadError, ((Job<JobPayloadError>)job).Payload.Error);
                        });
                }, JobHandlerBase.DefaultDataflowBlockOptions);

            lock (this.lockRegisteredJobHandlers)
            {
                var jobHandlerInfo = new JobHandlerInfo(payloadErrorHandler, CreateJobHandlerActionBlock(payloadErrorHandler, payloadErrorTypeTag.Value));
                this.registeredJobHandlers.Add(typeof(JobPayloadError), jobHandlerInfo);
            }

            this.keepInvisibleJobsTask = KeepInvisibleJobsAsync();
            return StartQueueAsync(queueMessageProducerSettings, cancellationToken);
        }

        /// <inheritdoc/>
        protected override async Task DisposeInternalAsync()
        {
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

        private void ThrowIfRunning()
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("Job Queue consumer already started.");
            }
        }

        private ActionBlock<IJob> CreateJobHandlerActionBlock<T>(IJobHandler<T> jobHandler)
            where T : JobPayload
        {
            return CreateJobHandlerActionBlock<T>(jobHandler, typeof(T).RegisterPayloadType());
        }

        /// <summary>
        /// Create a job handler action block.
        /// </summary>
        /// <typeparam name="T">Type of the payload.</typeparam>
        /// <param name="jobHandler">The type safe job handler instance.</param>
        /// <returns>A TPL action block.</returns>
        private ActionBlock<IJob> CreateJobHandlerActionBlock<T>(IJobHandler<T> jobHandler, string typeTag)
            where T : JobPayload
        {
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
                var maxHandlerRetriesOptions = jobHandlerOptions != null ? jobHandlerOptions.MaxHandlerRetries : jobPayloadOptions?.MaxHandlerRetries;
                var maxHandlerRetries = maxHandlerRetriesOptions ?? DefaultMaxRetries;

                var failed = false;
                var retries = 0;
                var cancelled = false;
                var expired = false;

                var start = Stopwatch.StartNew();
                var nowUtc = DateTime.UtcNow;

                await this.logger.OperationScopeAsync(
                    JobQueueConsumerMessage,
                    async (childLogger) =>
                    {
                        childLogger.FluentAddBaseValue(JobQueueLoggerConst.JobId, job.Id)
                            .FluentAddBaseValue(JobQueueLoggerConst.JobType, typeTag)
#if DEBUG
                            .FluentAddBaseValue(JobQueueLoggerConst.JobPayload, jobInstance.JobPayloadInfo.Payload)
#endif
                            .FluentAddValue(JobQueueLoggerConst.JobDequeuedDuration, nowUtc - jobInstance.DequeueTime);

                        // pass deserialized logger properties from the producer.
                        foreach (var kvp in jobTyped.Payload.LoggerProperties)
                        {
                            childLogger.FluentAddBaseValue(kvp.Key, kvp.Value?.ToString());
                        }

                        // expire timeout first on patyload options and then on the job hanlder instance.
                        var expireTimeout = jobPayloadOptions?.ExpireTimeout ?? jobHandlerOptions?.ExpireTimeout;

                        if (expireTimeout.HasValue == true &&
                            nowUtc > job.Created.Add(expireTimeout.Value))
                        {
                            await CompleteJobAsync(jobInstance, JobCompletedStatus.Failed | JobCompletedStatus.Removed | JobCompletedStatus.Expired, null);
                            childLogger.FluentAddValue(JobQueueLoggerConst.JobDidExpired, true)
                                .FluentAddValue(JobQueueLoggerConst.JobHandlerStatus, JobCompletedStatus.Failed | JobCompletedStatus.Removed | JobCompletedStatus.Expired);
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
                                        .FluentAddValue(JobQueueLoggerConst.JobQueueDuration, nowUtc - job.Created);
                                    var status = JobCompletedStatus.Succeeded;
                                    if (jobInstance.RetryTimeout.HasValue)
                                    {
                                        // keep this job message queue but retry again.
                                        await jobInstance.UpdateVisibilityAsync(jobInstance.RetryTimeout.Value, DisposeToken);
                                    }
                                    else
                                    {
                                        // remove this job message from the queue.
                                        status |= JobCompletedStatus.Removed;
                                    }

                                    childLogger.FluentAddValue(JobQueueLoggerConst.JobHandlerStatus, status);
                                    await CompleteJobAsync(jobInstance, status, null);
                                }
                            }
                        }
                    },
                    errCallback: async (err, logger) =>
                    {
                        // Note: if we arrive here the job handler implementation (or any initial step before the handler)
                        // throw an exception and so would cause different approaches on retrying or removing the payload
                        // to avoid another execution that we know could fail

                        failed = true;
                        logger.FluentAddValue(JobQueueLoggerConst.JobDidCancel, cancelled);

                        var status = JobCompletedStatus.Failed;

                        // if the job was cancelled due to timeout add the proper flag.
                        if (cancelled)
                        {
                            status = status | JobCompletedStatus.Cancelled;
                        }

                        try
                        {
                            ++jobInstance.JobPayloadInfo.Retries;
                            logger.FluentAddValue(JobQueueLoggerConst.JobRetries, jobInstance.JobPayloadInfo.Retries);

                            // Note: we will start by allowing 'custom' defined error handlers to handle the job error and decide
                            // what completion status is returned
                            var errorHandlerStatus = JobCompletedStatus.None;
                            if (jobHandlerOptions?.ErrorCallbacks != null)
                            {
                                foreach (var errorCallback in jobHandlerOptions.ErrorCallbacks)
                                {
                                    errorHandlerStatus = await errorCallback.HandleJobError(jobInstance, err, status, logger, DisposeToken);
                                    if (errorHandlerStatus != JobCompletedStatus.None)
                                    {
                                        // Any custom error handler that return != 'None' would stop.
                                        break;
                                    }
                                }
                            }

                            if (errorHandlerStatus != JobCompletedStatus.None)
                            {
                                status = errorHandlerStatus;

                                // If the custom error handler want 'Retry' we need some help to persist the 'Retries' property
                                // on the queued message
                                if (status.HasFlag(JobCompletedStatus.Retry))
                                {
                                    ++retries;
                                    await jobInstance.UpdateContentAsync(DisposeToken);
                                }
                            } // Check first if we reach the max numbers of retries
                            else if (jobInstance.JobPayloadInfo.Retries >= maxHandlerRetries)
                            {
                                // apply 'Exhaust' and 'Removed' flag status.
                                // This code path will force the payload to be removed permanently from the queue.
                                status = status | JobCompletedStatus.RetryExhausted | JobCompletedStatus.Removed;
                            }
                            else
                            {
                                ++retries;
                                status = status | JobCompletedStatus.Retry;
                                await jobInstance.UpdateContentAsync(DisposeToken);

                                // if the job handler options has a retry timeout, otherwise it will re appear
                                // on the consumer with the default setting how it was retrieved the first time.
                                TimeSpan? retryTimeout = jobInstance.RetryTimeout ?? jobHandlerOptions?.RetryTimeout;
                                if (retryTimeout.HasValue)
                                {
                                    await jobInstance.UpdateVisibilityAsync(retryTimeout.Value, DisposeToken);
                                }
                            }

                            logger.FluentAddValue(JobQueueLoggerConst.JobHandlerStatus, status);

                            // in all scenarios we would need to complete the job.
                            await CompleteJobAsync(jobInstance, status, err);
                        }
                        catch (Exception e)
                        {
                            var childLogger = logger.NewChildLogger();
                            childLogger.LogException("job_queue_consumer_error_callback_error", e);
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
                    jobHandlerMetrics.DequeuedDuration += nowUtc - jobInstance.DequeueTime;
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

            return actionBlock;
        }

        /// <summary>
        /// Start the queue message producer by pumping new queue messages into the custom block.
        /// </summary>
        private async Task StartQueueAsync(QueueMessageProducerSettings settings, CancellationToken cancellationToken)
        {
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, DisposeToken))
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var queueMessages = await Queue.GetMessagesAsync(settings.MessageCount, settings.VisibilityTimeout, settings.Timeout, cts.Token);
                    if (queueMessages.Any())
                    {
                        // Dequeue all the jobs to make them invisible for this instance.
                        var jobTasks = queueMessages.Select(queueMessage => CreateJob(queueMessage, settings.VisibilityTimeout))
                            .Select(job => job.JobHandlerInfo.ActionBlock.SendAsync(job, cts.Token))
                            .ToArray();

                        // Note: this next call could throttle if the bound capacity is reach.
                        await Task.WhenAll(jobTasks);
                    }
                }
            }
        }

        /// <summary>
        /// Create a job instance by parsing the JSON content and add it to the dequeued jobs
        /// </summary>
        private Job CreateJob(QueueMessage queueMessage, TimeSpan visibilityTimeout)
        {
            var (jobPayloadInfo, jobPayload) = CreateJobPayload(queueMessage);
            var jobPayloadType = jobPayload.GetType();
            var jobType = typeof(Job<>).MakeGenericType(jobPayloadType);
            JobHandlerInfo jobHandlerInfo;
            lock (this.lockRegisteredJobHandlers)
            {
                jobHandlerInfo = this.registeredJobHandlers[jobPayloadType];
            }

            var job = (Job)Activator.CreateInstance(jobType, jobHandlerInfo, Queue, queueMessage, visibilityTimeout, jobPayloadInfo, jobPayload);
            AddDequeuedJob(job);
            JobCreated?.Invoke(job);
            return job;
        }

        private Task CompleteJobAsync(Job job, JobCompletedStatus status, Exception error)
        {
            // Note: this method will remove the dequeued message from dictionary
            // and also complete the job by firing events
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
            string rawContent = null;
            try
            {
                rawContent = Encoding.UTF8.GetString(queueMessage.Content);
                var jobPayloadInfo = JobPayloadInfo.FromJson(rawContent);
                var jobPayload = JobPayloadHelpers.FromJson(jobPayloadInfo.TagType, jobPayloadInfo.Payload);
                lock (this.lockRegisteredJobHandlers)
                {
                    if (!this.registeredJobHandlers.ContainsKey(jobPayload.GetType()))
                    {
                        throw new NotSupportedException($"No registered job handler for type:{jobPayload.GetType()}");
                    }
                }

                return (jobPayloadInfo, jobPayload);
            }
            catch (Exception error)
            {
                this.logger
                    .WithValues(new LogValueSet()
                    {
                        { JobQueueLoggerConst.JobId, queueMessage.Id },
                        { JobQueueLoggerConst.JobRawContent, rawContent },
                        { JobQueueLoggerConst.JobQueueId, Queue.Id },
                    })
                    .LogException($"create_job_payload_failed", error);
                return (new JobPayloadInfo(null, null, DateTime.UtcNow), new JobPayloadError(error));
            }
        }

        private class JobHandlerInfo
        {
            internal JobHandlerInfo(IJobHandler jobHandler, ActionBlock<IJob> actionBlock)
            {
                JobHandler = jobHandler;
                ActionBlock = actionBlock;
            }

            public IJobHandler JobHandler { get; }

            public ActionBlock<IJob> ActionBlock { get; }
        }

        private abstract class Job : IJob
        {
            private readonly QueueMessage queueMessage;
            private DateTime invisibleTimeout;
            private int keepInvisibleCount;

            public Job(JobHandlerInfo jobHandlerInfo, IQueue queue, QueueMessage queueMessage, TimeSpan visibilityTimeout, JobPayloadInfo jobPayloadInfo)
            {
                JobHandlerInfo = jobHandlerInfo;
                Queue = queue;
                this.queueMessage = queueMessage;
                VisibilityTimeout = visibilityTimeout;
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

            /// <inheritdoc/>
            public TimeSpan? RetryTimeout { get; set; }

            public JobPayloadInfo JobPayloadInfo { get; }

            internal JobHandlerInfo JobHandlerInfo { get; }

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

            private static T GetValue<T>(T? value, T defaultValue)
                where T : struct
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
            public Job(JobHandlerInfo jobHandlerInfo, IQueue queue, QueueMessage queueMessage, TimeSpan visibilityTimeout, JobPayloadInfo jobPayloadInfo, T payload)
                : base(jobHandlerInfo, queue, queueMessage, visibilityTimeout, jobPayloadInfo)
            {
                Payload = payload;
                JobHandlerOptions = ((IJobHandler<T>)jobHandlerInfo.JobHandler)?.GetJobOptions(this);
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

            public TimeSpan DequeuedDuration { get; set; }

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
