// <copyright file="JobQueueProducer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs
{
    /// <summary>
    /// Implements interface IJobQueueProducer.
    /// </summary>
    public class JobQueueProducer : DisposableBase, IJobQueueProducer
    {
        private readonly IQueue queueMessage;
        private readonly Dictionary<string, JobQueueProducerMetrics> jobQueueProducerMetricsByTypeTag = new Dictionary<string, JobQueueProducerMetrics>();
        private readonly object lockJobQueueProducerMetrics = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="JobQueueProducer"/> class.
        /// </summary>
        /// <param name="queueMessage">A queue message instance.</param>
        public JobQueueProducer(IQueue queueMessage)
        {
            this.queueMessage = Requires.NotNull(queueMessage, nameof(queueMessage));
        }

        /// <inheritdoc/>
        public Task<QueueMessage> AddJobAsync(JobPayload job, JobPayloadOptions jobPayloadOptions, IDiagnosticsLogger logger, CancellationToken cancellationToken = default)
        {
            Requires.NotNull(job, nameof(job));
            Requires.NotNull(logger, nameof(logger));

            var tagType = JobPayloadHelpers.GetTypeTag(job.GetType());
            var payloadJson = job.ToJson();
            return logger.OperationScopeAsync(
                "job_queue_producer",
                async (childLogger) =>
                {
                    UpdateMetrics(tagType, m => ++m.Processed);
                    var json = new JobPayloadInfo(payloadJson, DateTime.UtcNow) { PayloadOptions = jobPayloadOptions }.ToJson();
                    var cloudMessage = await this.queueMessage.AddMessageAsync(Encoding.UTF8.GetBytes(json), jobPayloadOptions?.InitialVisibilityDelay, cancellationToken);
                    childLogger
                        .FluentAddValue(JobQueueLoggerConst.JobId, cloudMessage.Id)
                        .FluentAddValue(JobQueueLoggerConst.JobType, tagType)
                        .FluentAddValue(JobQueueLoggerConst.InitialVisibilityDelay, jobPayloadOptions?.InitialVisibilityDelay)
                        .FluentAddValue(JobQueueLoggerConst.ExpireTimeout, jobPayloadOptions?.ExpireTimeout);
                    return cloudMessage;
                },
                errCallback: (err, logger) =>
                {
                    UpdateMetrics(tagType, m => ++m.Failures);
                    return Task.FromResult<QueueMessage>(null);
                });
        }

        /// <inheritdoc/>
        public Dictionary<string, IJobQueueProducerMetrics> GetMetrics()
        {
            lock (this.lockJobQueueProducerMetrics)
            {
                var results = this.jobQueueProducerMetricsByTypeTag.ToDictionary(kvp => kvp.Key, kvp => kvp.Value as IJobQueueProducerMetrics);
                this.jobQueueProducerMetricsByTypeTag.Clear();
                return results;
            }
        }

        private void UpdateMetrics(string typeTag, Action<JobQueueProducerMetrics> updateCallback)
        {
            lock (this.lockJobQueueProducerMetrics)
            {
                JobQueueProducerMetrics jobQueueProducerMetrics;
                if (!this.jobQueueProducerMetricsByTypeTag.TryGetValue(typeTag, out jobQueueProducerMetrics))
                {
                    jobQueueProducerMetrics = new JobQueueProducerMetrics();
                    this.jobQueueProducerMetricsByTypeTag[typeTag] = jobQueueProducerMetrics;
                }

                updateCallback(jobQueueProducerMetrics);
            }
        }

        private class JobQueueProducerMetrics : IJobQueueProducerMetrics
        {
            public int Processed { get; set; }

            public int Failures { get; set; }
        }
    }
}
