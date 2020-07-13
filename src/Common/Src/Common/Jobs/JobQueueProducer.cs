// <copyright file="JobQueueProducer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
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
        private readonly IDiagnosticsLogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobQueueProducer"/> class.
        /// </summary>
        /// <param name="queueMessage">A queue message instance.</param>
        /// <param name="logger">Logger instance.</param>
        public JobQueueProducer(IQueue queueMessage, IDiagnosticsLogger logger)
        {
            this.queueMessage = Requires.NotNull(queueMessage, nameof(queueMessage));
            this.logger = Requires.NotNull(logger, nameof(logger));
        }

        /// <inheritdoc/>
        public Task AddJobAsync(JobPayload job, JobPayloadOptions jobPayloadOptions = null, CancellationToken cancellationToken = default)
        {
            Requires.NotNull(job, nameof(job));

            var payloadJson = job.ToJson();
            return this.logger.OperationScopeAsync(
            "job_queue_producer",
            async (childLogger) =>
            {
                var json = new JobPayloadInfo(payloadJson, DateTime.UtcNow) { PayloadOptions = jobPayloadOptions }.ToJson();
                var message = await this.queueMessage.AddMessageAsync(Encoding.UTF8.GetBytes(json), jobPayloadOptions?.InitialVisibilityDelay, cancellationToken);
                childLogger.FluentAddValue("jobId", message.Id);
            });
        }
    }
}
