// <copyright file="BillingPlanBatchConsumer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks.Payloads;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks
{
    /// <summary>
    /// Billing Plan Consumer.
    /// </summary>
    public class BillingPlanBatchConsumer : JobHandlerPayloadBase<BillingPlanBatchJobPayload>, IBillingPlanBatchConsumer
    {
        private static readonly TimeSpan ExpireDelay = TimeSpan.FromMinutes(20);

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingPlanBatchConsumer"/> class.
        /// </summary>
        /// <param name="billingSettings">The billing settings.</param>
        /// <param name="planManager">Target Plan Manager.</param>
        /// <param name="billingOverrideRepository">Target Billing Override Repository.</param>
        /// <param name="billingPlanSummaryProducer">Target Billing Plan Summary Producer.</param>
        /// <param name="billingPlanCleanupProducer">Target Billing Plan Cleanup Producer.</param>
        /// <param name="controlPlaneInfo">The control plane used for getting locations.</param>
        public BillingPlanBatchConsumer(
            BillingSettings billingSettings,
            IPlanManager planManager,
            IBillingOverrideRepository billingOverrideRepository,
            IBillingPlanSummaryProducer billingPlanSummaryProducer,
            IBillingPlanCleanupProducer billingPlanCleanupProducer,
            IControlPlaneInfo controlPlaneInfo)
        {
            BillingSettings = billingSettings;
            PlanManager = planManager;
            BillingOverrideRepository = billingOverrideRepository;
            BillingPlanSummaryProducer = billingPlanSummaryProducer;
            BillingPlanCleanupProducer = billingPlanCleanupProducer;
            ControlPlaneInfo = controlPlaneInfo;
        }

        private BillingSettings BillingSettings { get; }

        private IPlanManager PlanManager { get; }

        private IBillingOverrideRepository BillingOverrideRepository { get; }

        private IBillingPlanSummaryProducer BillingPlanSummaryProducer { get; }

        private IBillingPlanCleanupProducer BillingPlanCleanupProducer { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        /// <inheritdoc/>
        protected override Task HandleJobAsync(BillingPlanBatchJobPayload payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync(
                $"{BillingLoggingConstants.BillingPlanBatchTask}_handle",
                async (childLogger) =>
                {
                    childLogger.FluentAddValue(BillingLoggingConstants.Shard, payload.PlanShard);

                    int producerCount = await BillingSettings.V2ConcurrentJobProducerCountAsync(childLogger);

                    foreach (var location in ControlPlaneInfo.Stamp.DataPlaneLocations)
                    {
                        if (producerCount > 1)
                        {
                            await QueuePlansParallel(payload.PlanShard, producerCount, location, childLogger, cancellationToken);
                        }
                        else
                        {
                            await QueuePlansSequential(payload.PlanShard, location, childLogger, cancellationToken);
                        }
                    }
                });
        }

        private async Task QueuePlansSequential(string shard, AzureLocation location, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            // Fetch list of plans overrides
            var overrides = (await BillingOverrideRepository.QueryAsync(q => q, logger)).ToList();
            var globalOverrides = overrides.Where(x => x.PlanId == null && x.Subscription == null).Select(SelectOverridePayload).ToList();
            var planOverrides = overrides.Where(x => x.PlanId != null).GroupBy(x => x.PlanId).ToDictionary(x => x.Key.Value, x => x.ToList().Select(SelectOverridePayload));
            var subscriptionOverrides = overrides.Where(x => x.Subscription != null).GroupBy(x => x.Subscription).ToDictionary(x => x.Key, x => x.ToList().Select(SelectOverridePayload));

            await PlanManager.GetBillablePlansByShardAsync(
                shard,
                location,
                async (plan, childLogger) =>
                {
                    childLogger.FluentAddValue(BillingLoggingConstants.PlanId, plan.Id);

                    await QueueJob(plan, globalOverrides, planOverrides, subscriptionOverrides, childLogger, cancellationToken);
                },
                null,
                logger);
        }

        private async Task QueuePlansParallel(string shard, int concurrentJobProducerCount, AzureLocation location, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            // Fetch list of plans overrides
            var overrides = (await BillingOverrideRepository.QueryAsync(q => q, logger)).ToList();
            var globalOverrides = overrides.Where(x => x.PlanId == null && x.Subscription == null).Select(SelectOverridePayload).ToList();
            var planOverrides = overrides.Where(x => x.PlanId != null).GroupBy(x => x.PlanId).ToDictionary(x => x.Key.Value, x => x.ToList().Select(SelectOverridePayload));
            var subscriptionOverrides = overrides.Where(x => x.Subscription != null).GroupBy(x => x.Subscription).ToDictionary(x => x.Key, x => x.ToList().Select(SelectOverridePayload));

            // queue the summary jobs
            var queueJobBlock = new ActionBlock<VsoPlan>(
                async plan => await QueueJob(plan, globalOverrides, planOverrides, subscriptionOverrides, logger, cancellationToken),
                new ExecutionDataflowBlockOptions { BoundedCapacity = 1000, MaxDegreeOfParallelism = concurrentJobProducerCount });

            // the producer which retrieves the plans within the shard
            async Task Producer()
            {
                // Fetch list of plans
                await PlanManager.GetBillablePlansByShardAsync(
                    shard,
                    location,
                    async (plan, childLogger) =>
                    {
                        childLogger.FluentAddValue(BillingLoggingConstants.PlanId, plan.Id);

                        await queueJobBlock.SendAsync(plan);
                    },
                    null,
                    logger);

                queueJobBlock.Complete();
            }

            // start the dataflow
            await Task.WhenAll(Producer(), queueJobBlock.Completion);
        }

        private Task QueueJob(VsoPlan plan, IEnumerable<BillingPlanSummaryOverrideJobPayload> globalOverrides, Dictionary<Guid, IEnumerable<BillingPlanSummaryOverrideJobPayload>> planOverrides, Dictionary<string, IEnumerable<BillingPlanSummaryOverrideJobPayload>> subscriptionOverrides, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync(
                $"{BillingLoggingConstants.BillingPlanBatchTask}_queue_job",
                async (childLogger) =>
                {
                    childLogger.FluentAddValue(BillingLoggingConstants.PlanId, plan.Id);

                    // Set messages to become visable at the time of the hour
                    var currentTimeOfDay = DateTime.UtcNow.TimeOfDay;
                    var nextFullHour = TimeSpan.FromHours(Math.Ceiling(currentTimeOfDay.TotalHours));
                    var initialVisibilityDelay = nextFullHour - currentTimeOfDay;
                    var nextHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0).AddHours(1);
                    var partner = plan.Partner;

                    await BillingPlanSummaryProducer.PublishJobAsync(
                        plan.Id,
                        plan.Plan,
                        nextHour,
                        partner,
                        BuildOverrides(Guid.Parse(plan.Id), plan.Plan.Subscription, globalOverrides, planOverrides, subscriptionOverrides),
                        new JobPayloadOptions { InitialVisibilityDelay = initialVisibilityDelay, ExpireTimeout = initialVisibilityDelay + ExpireDelay },
                        childLogger.NewChildLogger(),
                        cancellationToken);

                    // also queue cleanup and archiving job
                    var cleanupDelay = initialVisibilityDelay.Add(TimeSpan.FromMinutes(20));

                    await BillingPlanCleanupProducer.PublishJobAsync(
                        plan.Id,
                        nextHour,
                        new JobPayloadOptions { InitialVisibilityDelay = cleanupDelay, ExpireTimeout = cleanupDelay + ExpireDelay },
                        childLogger.NewChildLogger(),
                        cancellationToken);
                });
        }

        private IEnumerable<BillingPlanSummaryOverrideJobPayload> BuildOverrides(
            Guid planId,
            string subscription,
            IEnumerable<BillingPlanSummaryOverrideJobPayload> globalOverrides,
            IDictionary<Guid, IEnumerable<BillingPlanSummaryOverrideJobPayload>> planOverrides,
            IDictionary<string, IEnumerable<BillingPlanSummaryOverrideJobPayload>> subscriptionOverrides)
        {
            return globalOverrides
                .Concat(planOverrides.GetValueOrDefault(planId, () => Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>()))
                .Concat(subscriptionOverrides.GetValueOrDefault(subscription, () => Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>()));
        }

        private BillingPlanSummaryOverrideJobPayload SelectOverridePayload(BillingOverride x)
        {
            return new BillingPlanSummaryOverrideJobPayload { Priority = x.Priority, BillingOverrideState = x.BillingOverrideState, EndTime = x.EndTime, StartTime = x.StartTime, Sku = x.Sku };
        }
    }
}
