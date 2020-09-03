// <copyright file="BillingPlanCleanupConsumer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks.Payloads;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks
{
    /// <summary>
    /// Billing Plan Consumer.
    /// </summary>
    public class BillingPlanCleanupConsumer : JobHandlerPayloadBase<BillScrubberRequest>, IBillingPlanCleanupConsumer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BillingPlanCleanupConsumer"/> class.
        /// </summary>
        /// <param name="billingSettings">Billing settings.</param>
        /// <param name="billSummaryScrubber">Target Bill Summary scrubber.</param>
        public BillingPlanCleanupConsumer(
            BillingSettings billingSettings,
            IBillSummaryScrubber billSummaryScrubber)
            : base(billingSettings.ConcurrentJobConsumerCount)
        {
            BillSummaryScrubber = billSummaryScrubber;
        }

        private IBillSummaryScrubber BillSummaryScrubber { get; }

        /// <inheritdoc/>
        protected override Task HandleJobAsync(BillScrubberRequest payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync(
                $"{BillingLoggingConstants.BillingPlanCleanupTask}_handle",
                async (childLogger) =>
                {
                    childLogger.FluentAddValue(BillingLoggingConstants.PlanId, payload.PlanId);

                    await BillSummaryScrubber.ScrubBillSummariesForPlanAsync(payload, childLogger.NewChildLogger());
                });
        }
    }
}
