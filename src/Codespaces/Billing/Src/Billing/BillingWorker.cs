// <copyright file="BillingWorker.cs" company="Microsoft">
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
    /// Backgroud Worker for emitting billing summary records.
    /// </summary>
    public class BillingWorker : BackgroundService
    {
        private readonly BillingSettings billingSettings;
        private readonly IBillingService billingService;
        private readonly IDiagnosticsLogger logger;
        private readonly double taskRunIntervalMinutes = 60;

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingWorker"/> class.
        /// </summary>
        /// <param name="service">IBillingService.</param>
        /// <param name="diagnosticsLogger">IDiagnosticLogger.</param>
        public BillingWorker(
            BillingSettings billingSettings,
            IBillingService service,
            IDiagnosticsLogger diagnosticsLogger)
        {
            this.billingSettings = billingSettings;
            billingService = service;
            logger = diagnosticsLogger.NewChildLogger();
        }

        /// <summary>
        /// Executes a background Task.
        /// </summary>
        /// <param name="cancellationToken">Notification object for stopping the task.</param>
        /// <returns>Task.</returns>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            logger.FluentAddBaseValue("Service", "billingservices");
            logger.LogInfo("Billing Worker is initializing.");
            cancellationToken.Register(() => logger.LogInfo("Billing Worker was cancelled."));

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var duration = logger.StartDuration();

                    if (await billingSettings.V1IsEnabledAsync(logger))
                    {
                        await logger.OperationScopeAsync(
                             $"billWorker_begin",
                             async (childLogger) =>
                             {
                                 await billingService.GenerateBillingSummaryAsync(cancellationToken);
                             }, swallowException: true);
                    }

                    logger.AddDuration(duration).LogInfo(GetType().FormatLogMessage(nameof(ExecuteAsync)));
                    var remainingTime = taskRunIntervalMinutes - TimeSpan.FromMilliseconds(duration.Elapsed.TotalMilliseconds).TotalMinutes;
                    await Task.Delay(TimeSpan.FromMinutes(remainingTime > 0 ? remainingTime : 0));
                }
                catch (Exception ex)
                {
                    logger.LogErrorWithDetail("Error executing Billing Worker.", ex.Message);
                }
            }

            logger.LogInfo("Billing Worker is stopping.");
        }
    }
}
