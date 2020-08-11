// <copyright file="BillingPlanBatchProducer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks.Payloads;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks
{
    /// <summary>
    /// Defines a task which is designed to place jobs on the queue to fetch plans for billing processing.
    /// </summary>
    public class BillingPlanBatchProducer : IBillingPlanBatchProducer
    {
        private System.TimeSpan expirationTime = System.TimeSpan.FromMinutes(15);

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingPlanBatchProducer"/> class.
        /// </summary>
        /// <param name="jobQueueProducerFactory">Target Job Queue Producer Factory.</param>
        public BillingPlanBatchProducer(
            IJobQueueProducerFactory jobQueueProducerFactory)
        {
            JobQueueProducer = jobQueueProducerFactory.GetOrCreate(BillingLoggingConstants.BillingPlanBatchQueue);
        }

        private IJobQueueProducer JobQueueProducer { get; }

        /// <inheritdoc/>
        public Task PublishJobAsync(string shard, JobPayloadOptions jobPayloadOptions, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync(
                $"{BillingLoggingConstants.BillingPlanBatchTask}_publish",
                async (childLogger) =>
                {
                    // Push job onto queue
                    await JobQueueProducer.AddJobAsync(new BillingPlanBatchJobPayload() { PlanShard = shard }, jobPayloadOptions, cancellationToken);
                });
        }
    }
}
