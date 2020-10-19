// <copyright file="PartnerWorker.cs" company="Microsoft">
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
    ///  Background worker that's used to transfer billing summaries to partners.
    /// </summary>
    public class PartnerWorker : BackgroundService
    {
        private readonly BillingSettings billingSettings;
        private readonly IPartnerService partnerService;
        private readonly IDiagnosticsLogger logger;
        private readonly string name;

        private readonly double interval = 60;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartnerWorker"/> class.
        /// </summary>
        /// <param name="partnerService">the partner service that runs the actual operation.</param>
        /// <param name="diagnosticsLogger">the logger.</param>
        /// <param name="name">The service name.</param>
        /// <param name="interval">The interval.</param>
        public PartnerWorker(
            BillingSettings billingSettings,
            IPartnerService partnerService,
            IDiagnosticsLogger diagnosticsLogger,
            string name,
            double interval)
        {
            this.billingSettings = billingSettings;
            this.partnerService = partnerService;
            this.logger = diagnosticsLogger.NewChildLogger();
            this.name = name;
            this.interval = interval;
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInfo($"{this.name}_partner_worker_start_worker");

            while (!cancellationToken.IsCancellationRequested)
            {
                var duration = this.logger.StartDuration();

                if (await billingSettings.V1WorkersAreEnabledAsync(logger) &&
                    await billingSettings.V1PartnerTransmissionIsEnabledAsync(logger))
                {
                    await this.logger.OperationScopeAsync(
                      $"{this.name}_partner_worker_begin",
                      async (childLogger) =>
                      {
                          // Do the actual work
                          await this.partnerService.Execute(cancellationToken);

                          // await billingSummarySubmissionService.CheckForBillingSubmissionErorrs(cancellationToken);
                      }, swallowException: true);
                }

                // Delay for 1 hour
                var remainingTime = this.interval - TimeSpan.FromMilliseconds(duration.Elapsed.TotalMilliseconds).TotalMinutes;
                await Task.Delay(TimeSpan.FromMinutes(remainingTime > 0 ? remainingTime : 0));
            }
        }
    }
}