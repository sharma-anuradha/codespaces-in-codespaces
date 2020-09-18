// <copyright file="BillingManagementProducer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks.Payloads;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks
{
    /// <summary>
    /// Registeres any jobs that need to be run on warmup.
    /// </summary>
    public class BillingManagementProducer : IBillingManagementProducer
    {
        private TimeSpan expirationDelay = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingManagementProducer"/> class.
        /// </summary>
        /// <param name="jobQueueProducerFactory">Target job Queue Producer Factory.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="billingSettings">Billing settings.</param>
        public BillingManagementProducer(
            IJobQueueProducerFactory jobQueueProducerFactory,
            IClaimedDistributedLease claimedDistributedLease,
            BillingSettings billingSettings)
        {
            JobQueueProducer = jobQueueProducerFactory.GetOrCreate(BillingLoggingConstants.BillingManagermentQueue);
            ClaimedDistributedLease = claimedDistributedLease;
            BillingSettings = billingSettings;
        }

        private IJobQueueProducer JobQueueProducer { get; }

        private IClaimedDistributedLease ClaimedDistributedLease { get; }

        private BillingSettings BillingSettings { get; }

        /// <inheritdoc/>
        public Task PublishJobAsync(IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync(
                $"{BillingLoggingConstants.BillingManagementTask}_publish",
                async (childLogger) =>
                {
                    if (await BillingSettings.V2BillingManagementProducerIsEnabledAsync(childLogger))
                    {
                        // Obtain lease so trigger task is only added once
                        using (var lease = await ObtainLeaseAsync($"{BillingLoggingConstants.BillingManagementTask}-lease", TimeSpan.FromHours(1), childLogger))
                        {
                            if (lease != null)
                            {
                                // Schedule to triggered at 40min past the hour
                                var currentTime = DateTime.UtcNow;
                                var taretTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, 40, 0);
                                var initialVisibilityDelay = GetTimeSpanBetweenDates(currentTime, taretTime);

                                // Push job onto queue
                                await JobQueueProducer.AddJobAsync(
                                    new BillingManagementJobPayload(),
                                    new JobPayloadOptions { InitialVisibilityDelay = initialVisibilityDelay, ExpireTimeout = initialVisibilityDelay + expirationDelay },
                                    logger,
                                    cancellationToken);
                            }
                        }
                    }
                });
        }

        private Task<IDisposable> ObtainLeaseAsync(string leaseName, TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return ClaimedDistributedLease.Obtain("billing-leases", leaseName, claimSpan, logger);
        }

        private TimeSpan GetTimeSpanBetweenDates(DateTime baseTime, DateTime targetTime)
        {
            var difference = targetTime - baseTime;
            return difference > TimeSpan.Zero ? difference : TimeSpan.Zero;
        }
    }
}
