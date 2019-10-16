// <copyright file="BillingSummarySubmissionWorker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    ///  Background worker that's used to transfer billing summaries to the PaV2 commerce queue/storage system
    /// </summary>
    public class BillingSummarySubmissionWorker : BackgroundService
    {
        private readonly IBillingSummarySubmissionService billingSummarySubmissionService;
        private readonly IDiagnosticsLogger logger;

        private readonly double taskRunIntervalMinutes = 60;

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingSummarySubmissionWorker"/> class.
        /// </summary>
        /// <param name="billingSummarySubmissionService">the billing service that runs the actual operation</param>
        /// <param name="diagnosticsLogger">the logger</param>
        public BillingSummarySubmissionWorker(IBillingSummarySubmissionService billingSummarySubmissionService, IDiagnosticsLogger diagnosticsLogger)
        {
            this.billingSummarySubmissionService = billingSummarySubmissionService;
            this.logger = diagnosticsLogger;
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            logger.LogInfo("BillingSummarySubmission worker is starting up");

            while (!cancellationToken.IsCancellationRequested)
            {
                logger.LogInfo("BillingSummarySubmission worker is processing");
                var duration = logger.StartDuration();

                // Do the actual work
                await billingSummarySubmissionService.ProcessBillingSummariesAsync(cancellationToken);

                // Log
                logger.AddDuration(duration).LogInfo(GetType().FormatLogMessage(nameof(ExecuteAsync)));
                logger.LogInfo("BillingSummarySubmission worker is done processing a cycle");

                // Delay for 1 hour
                var remainingTime = taskRunIntervalMinutes - TimeSpan.FromMilliseconds(duration.Elapsed.TotalMilliseconds).TotalMinutes;
                await Task.Delay(TimeSpan.FromMinutes(remainingTime > 0 ? remainingTime : 0));
            }
        }
    }
}