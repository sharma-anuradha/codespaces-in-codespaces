// <copyright file="IBillingPlanBatchProducer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks
{
    /// <summary>
    /// Defines a task which is designed to place jobs on the queue to fetch plans for billing processing.
    /// </summary>
    public interface IBillingPlanBatchProducer
    {
        /// <summary>
        /// Publish job of Billing Plan Batch queue.
        /// </summary>
        /// <param name="shard">Target Shard.</param>
        /// <param name="jobPayloadOptions">Target Job Payload Options.</param>
        /// <param name="logger">Target Logger.</param>
        /// <param name="cancellationToken">Target Cancellation Token.</param>
        /// <returns>Running task.</returns>
        Task PublishJobAsync(
            string shard,
            JobPayloadOptions jobPayloadOptions,
            IDiagnosticsLogger logger,
            CancellationToken cancellationToken);
    }
}
