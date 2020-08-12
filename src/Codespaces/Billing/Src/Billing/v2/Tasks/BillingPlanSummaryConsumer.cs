// <copyright file="BillingPlanSummaryConsumer.cs" company="Microsoft">
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
    public class BillingPlanSummaryConsumer : JobHandlerPayloadBase<BillingSummaryRequest>, IBillingPlanSummaryConsumer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BillingPlanSummaryConsumer"/> class.
        /// </summary>
        /// <param name="billingSettings">Billing Settings.</param>
        /// <param name="billSummaryGenerator">Target Bill Summary Generator.</param>
        public BillingPlanSummaryConsumer(
            BillingSettings billingSettings,
            IBillSummaryGenerator billSummaryGenerator)
            : base(billingSettings.ConcurrentJobConsumerCount)
        {
            BillSummaryGenerator = billSummaryGenerator;
        }

        private IBillSummaryGenerator BillSummaryGenerator { get; }

        /// <inheritdoc/>
        protected override Task HandleJobAsync(BillingSummaryRequest payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync(
                $"{BillingLoggingConstants.BillingPlanSummaryTask}_handle",
                async (childLogger) =>
                {
                    await BillSummaryGenerator.GenerateBillingSummaryAsync(payload, childLogger);
                });
        }
    }
}
