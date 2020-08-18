// <copyright file="BillingRegisterJobs.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Registeres any jobs that need to be run on warmup.
    /// </summary>
    public class BillingRegisterJobs : IAsyncBackgroundWarmup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BillingRegisterJobs"/> class.
        /// </summary>
        /// <param name="billingSettings">Billing Settings..</param>
        /// <param name="billingManagementConsumer">Target Billing Management Consumer.</param>
        /// <param name="billingPlanBatchConsumer">Target Billing Plan Batch Consumer.</param>
        /// <param name="billingPlanSummaryConsumer">Target Billing Plan Summary Consumer.</param>
        /// <param name="billingPlanCleanupConsumer">Target Billing Plan Cleanup Consumer.</param>
        /// <param name="billingManagementProducer">Target Billing Management Producer.</param>
        /// <param name="jobQueueConsumerFactory">Target Job Queue Consumer Factory.</param>
        /// <param name="taskHelper">Target Task Helper.</param>
        public BillingRegisterJobs(
            BillingSettings billingSettings,
            IBillingManagementConsumer billingManagementConsumer,
            IBillingPlanBatchConsumer billingPlanBatchConsumer,
            IBillingPlanSummaryConsumer billingPlanSummaryConsumer,
            IBillingPlanCleanupConsumer billingPlanCleanupConsumer,
            IBillingManagementProducer billingManagementProducer,
            IJobQueueConsumerFactory jobQueueConsumerFactory,
            ITaskHelper taskHelper)
        {
            BillingSettings = billingSettings;
            BillingManagementConsumer = billingManagementConsumer;
            BillingPlanBatchConsumer = billingPlanBatchConsumer;
            BillingPlanSummaryConsumer = billingPlanSummaryConsumer;
            BillingPlanCleanupConsumer = billingPlanCleanupConsumer;
            BillingManagementProducer = billingManagementProducer;
            JobQueueConsumerFactory = jobQueueConsumerFactory;
            TaskHelper = taskHelper;
        }

        private BillingSettings BillingSettings { get; }

        private IBillingManagementConsumer BillingManagementConsumer { get; }

        private IBillingPlanBatchConsumer BillingPlanBatchConsumer { get; }

        private IBillingPlanSummaryConsumer BillingPlanSummaryConsumer { get; }

        private IBillingPlanCleanupConsumer BillingPlanCleanupConsumer { get; }

        private IBillingManagementProducer BillingManagementProducer { get; }

        private IJobQueueConsumerFactory JobQueueConsumerFactory { get; }

        private ITaskHelper TaskHelper { get; }

        /// <inheritdoc/>
        public async Task BackgroundWarmupCompletedAsync(IDiagnosticsLogger logger)
        {
            if (await BillingSettings.V2BillingManagementProducerIsEnabledAsync(logger))
            {
                // Register: Queue handlers
                JobQueueConsumerFactory
                    .GetOrCreate(BillingLoggingConstants.BillingManagermentQueue)
                    .RegisterJobHandler(BillingManagementConsumer)
                    .Start();
                JobQueueConsumerFactory
                    .GetOrCreate(BillingLoggingConstants.BillingPlanBatchQueue)
                    .RegisterJobHandler(BillingPlanBatchConsumer)
                    .Start();
                JobQueueConsumerFactory
                    .GetOrCreate(BillingLoggingConstants.BillingPlanSummaryQueue)
                    .RegisterJobHandler(BillingPlanSummaryConsumer)
                    .Start();
                JobQueueConsumerFactory
                    .GetOrCreate(BillingLoggingConstants.BillingPlanCleanupQueue)
                    .RegisterJobHandler(BillingPlanCleanupConsumer)
                     .Start();

                // Job: Start Billing Management Producer
                TaskHelper.RunBackgroundLoop(
                    $"{BillingLoggingConstants.BillingManagementTask}_run",
                    async (childLogger) =>
                    {
                        await BillingManagementProducer.PublishJobAsync(childLogger, CancellationToken.None);
                        return true;
                    },
                    TimeSpan.FromMinutes(5));
            }
        }
    }
}
