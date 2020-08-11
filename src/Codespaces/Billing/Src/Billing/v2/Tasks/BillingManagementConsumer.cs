// <copyright file="BillingManagementConsumer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks.Payloads;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks
{
    /// <summary>
    /// Billing Management Consumer.
    /// </summary>
    public class BillingManagementConsumer : JobHandlerPayloadBase<BillingManagementJobPayload>, IBillingManagementConsumer
    {
        private static readonly TimeSpan ExpireDelay = TimeSpan.FromMinutes(15);

        private readonly IEnumerable<string> shards = new[] { "a", "b", "c", "d", "e", "f", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }.Shuffle();

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingManagementConsumer"/> class.
        /// </summary>
        /// <param name="billingPlanBatchProducer">Target Billing Plan Batch Producer.</param>
        public BillingManagementConsumer(
            IBillingPlanBatchProducer billingPlanBatchProducer)
        {
            BillingPlanBatchProducer = billingPlanBatchProducer;
        }

        private IBillingPlanBatchProducer BillingPlanBatchProducer { get; }

        /// <inheritdoc/>
        protected override Task HandleJobAsync(BillingManagementJobPayload payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync(
                $"{BillingLoggingConstants.BillingManagementTask}_handle",
                async (childLogger) =>
                {
                    var i = 0;
                    foreach (var shard in shards)
                    {
                        var initialDelay = TimeSpan.FromSeconds(10 * i++);

                        // Push job onto queue (delay 30s between)
                        await BillingPlanBatchProducer.PublishJobAsync(
                            shard,
                            new JobPayloadOptions { InitialVisibilityDelay = initialDelay, ExpireTimeout = initialDelay + ExpireDelay },
                            childLogger,
                            cancellationToken);
                    }
                });
        }
    }
}
