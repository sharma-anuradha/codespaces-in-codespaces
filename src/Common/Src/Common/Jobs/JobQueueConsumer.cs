// <copyright file="JobQueueConsumer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Policy;
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
        private readonly BroadcastBlock<IJob> blockJobs = new BroadcastBlock<IJob>(job => job);
        private readonly IDiagnosticsLogger logger;
        private readonly HashSet<Type> registeredPayloadTypes = new HashSet<Type>();
        private readonly object lockRegisteredPayloadTypes = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="JobQueueConsumer"/> class.
        /// </summary>
        /// <param name="queueMessageProducer">A queue producer instance.</param>
        /// <param name="logger">Logger instance.</param>
        public JobQueueConsumer(IQueueMessageProducer queueMessageProducer, IDiagnosticsLogger logger)
        {
            Requires.NotNull(queueMessageProducer, nameof(queueMessageProducer));
            this.logger = Requires.NotNull(logger, nameof(logger));

            var jobTransformerBlock = new TransformBlock<(QueueMessage, TimeSpan), IJob>((queueMessageInfo) =>
            {
                var payloadTuple = CreateJobPayload(queueMessageInfo.Item1);
                var jobType = typeof(Job<>).MakeGenericType(payloadTuple.Item2.GetType());
                return (IJob)Activator.CreateInstance(jobType, queueMessageProducer.Queue, queueMessageInfo, payloadTuple.Item1, payloadTuple.Item2);
            });

            queueMessageProducer.Messages.LinkTo(jobTransformerBlock);
            jobTransformerBlock.LinkTo(blockJobs);
        }

        /// <inheritdoc/>
        public void RegisterJobHandler<T>(IJobHandler<T> jobHandler, ExecutionDataflowBlockOptions dataflowBlockOptions)
            where T : JobPayload
        {
            // register the payload type on the cache.
            typeof(T).RegisterPayloadType();

            // register on this job consumer
            lock (this.lockRegisteredPayloadTypes)
            {
                this.registeredPayloadTypes.Add(typeof(T));
            }

            // We have to have a wrapper to work with IJob instead of T
            Func<IJob, Task> actionWrapper = (job) =>
            {
                var jobInstance = (Job)job;
                var jobPayloadOptions = jobInstance.JobPayloadInfo.PayloadOptions;

                return this.logger.OperationScopeAsync(
                    "job_queue_consumer",
                    (childLogger) =>
                    {
                        childLogger.FluentAddValue("jobId", job.Id);
                        using (var cts = CancellationTokenSource.CreateLinkedTokenSource(DisposeToken))
                        {
                            if (jobPayloadOptions?.HandlerTimout.HasValue == true)
                            {
                                cts.CancelAfter((int)jobPayloadOptions?.HandlerTimout.Value.TotalMilliseconds);
                            }

                            return jobHandler.HandleJobAsync((IJob<T>)job, childLogger, cts.Token);
                        }
                    },
                    errCallback: async (err, logger) =>
                    {
                        try
                        {
                            ++jobInstance.JobPayloadInfo.Retries;
                            if (jobPayloadOptions?.MaxHandlerRetries.HasValue == true &&
                                jobInstance.JobPayloadInfo.Retries >= jobPayloadOptions?.MaxHandlerRetries.Value)
                            {
                                await jobInstance.DisposeAsync();
                            }
                            else
                            {
                                await jobInstance.UpdateContentAsync(DisposeToken);
                            }
                        }
                        catch
                        {
                        }
                    },
                    swallowException: true);
            };

            // create the action block that executes the handler wrapper
            var actionBlock = new ActionBlock<IJob>((job) => actionWrapper(job), dataflowBlockOptions);

            // Link with Predicate - only if a job is of type T
            this.blockJobs.LinkTo(actionBlock, predicate: (job) => job is IJob<T>);
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
                    if (!this.registeredPayloadTypes.Contains(jobPayload.GetType()))
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

        private class Job : IJob
        {
            private readonly IQueue queue;
            private readonly QueueMessage message;

            public Job(IQueue queue, (QueueMessage, TimeSpan) messageInfo, JobPayloadInfo jobPayloadInfo)
            {
                this.queue = queue;
                this.message = messageInfo.Item1;
                VisibilityTimeout = messageInfo.Item2;
                JobPayloadInfo = jobPayloadInfo;
            }

            /// <inheritdoc/>
            public string Id => this.message.Id;

            /// <inheritdoc/>
            public TimeSpan VisibilityTimeout { get; }

            /// <inheritdoc/>
            public DateTime Created => JobPayloadInfo.Created;

            /// <inheritdoc/>
            public int Retries => JobPayloadInfo.Retries;

            public JobPayloadInfo JobPayloadInfo { get; }

            public async ValueTask DisposeAsync()
            {
                await this.queue.DeleteMessageAsync(this.message, default);
            }

            public Task UpdateAsync(TimeSpan visibilityTimeout, CancellationToken cancellationToken)
            {
                return this.queue.UpdateMessageAsync(this.message, false, visibilityTimeout, cancellationToken);
            }

            public Task UpdateContentAsync(CancellationToken cancellationToken)
            {
                message.Content = Encoding.UTF8.GetBytes(JobPayloadInfo.ToJson());
                return this.queue.UpdateMessageAsync(this.message, true, VisibilityTimeout, cancellationToken);
            }
        }

        private class Job<T> : Job, IJob<T>
            where T : JobPayload
        {
            public Job(IQueue queue, (QueueMessage, TimeSpan) message, JobPayloadInfo jobPayloadInfo, T payload)
                : base(queue, message, jobPayloadInfo)
            {
                Payload = payload;
            }

            /// <inheritdoc/>
            public T Payload { get; }
        }
    }
}
