// <copyright file="BillingSummarySubmissionWorker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    ///  Background worker that's used to transfer billing summaries to the PaV2 commerce queue/storage system.
    /// </summary>
    public class BillingSummarySubmissionWorker : BackgroundService
    {
        private readonly BillingSettings billingSettings;
        private readonly IBillingSummarySubmissionService billingSummarySubmissionService;
        private readonly IDiagnosticsLogger logger;

        private readonly double taskRunIntervalMinutes = 60;

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingSummarySubmissionWorker"/> class.
        /// </summary>
        /// <param name="billingSummarySubmissionService">the billing service that runs the actual operation.</param>
        /// <param name="diagnosticsLogger">the logger.</param>
        public BillingSummarySubmissionWorker(
            BillingSettings billingSettings,
            IBillingSummarySubmissionService billingSummarySubmissionService,
            IDiagnosticsLogger diagnosticsLogger)
        {
            this.billingSettings = billingSettings;
            this.billingSummarySubmissionService = billingSummarySubmissionService;
            logger = diagnosticsLogger.NewChildLogger();
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            logger.LogInfo("billingsub_worker_start_worker");

            while (!cancellationToken.IsCancellationRequested)
            {
                var duration = logger.StartDuration();

                if (await billingSettings.V1IsEnabledAsync(logger) &&
                    await billingSettings.V1TransmissionIsEnabledAsync(logger))
                {
                    await logger.OperationScopeAsync(
                      $"billingsub_worker_begin",
                      async (childLogger) =>
                      {
                          // Do the actual work
                          await billingSummarySubmissionService.ProcessBillingSummariesAsync(cancellationToken);
                          await billingSummarySubmissionService.CheckForBillingSubmissionErorrs(cancellationToken);
                      }, swallowException: true);
                }

                // Delay for 1 hour
                var remainingTime = taskRunIntervalMinutes - TimeSpan.FromMilliseconds(duration.Elapsed.TotalMilliseconds).TotalMinutes;
                await Task.Delay(TimeSpan.FromMinutes(remainingTime > 0 ? remainingTime : 0));
            }
        }
    }
}