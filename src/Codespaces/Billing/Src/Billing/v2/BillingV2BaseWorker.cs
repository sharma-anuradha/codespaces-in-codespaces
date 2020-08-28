// <copyright file="BillingV2BaseWorker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks.Payloads;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// A background worker for billing V2.
    /// </summary>
    public abstract class BillingV2BaseWorker : BackgroundService
    {
        private const string LogBaseName = "billing_v2_worker";
        private const string LeaseContainer = "billing-leases";

        private readonly BillingSettings billingSettings;
        private readonly IControlPlaneInfo controlPlaneInfo;
        private readonly IPlanManager planManager;
        private readonly IClaimedDistributedLease claimedDistributedLease;
        private readonly ITaskHelper taskHelper;

        private readonly IEnumerable<string> shards = new[] { "a", "b", "c", "d", "e", "f", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }.Shuffle();

        public BillingV2BaseWorker(
            BillingSettings billingSettings,
            IControlPlaneInfo controlPlaneInfo,
            IPlanManager planManager,
            IClaimedDistributedLease claimedDistributedLease,
            ITaskHelper taskHelper)
        {
            this.billingSettings = billingSettings;
            this.controlPlaneInfo = controlPlaneInfo;
            this.planManager = planManager;
            this.claimedDistributedLease = claimedDistributedLease;
            this.taskHelper = taskHelper;
        }

        protected Task ForEachPlan(string name, Func<VsoPlan, IDiagnosticsLogger, Task> action, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_{name}_for_each_plan",
                (childLogger) =>
                {
                    logger.LogInfo($"{LogBaseName}_{name}_for_each_plan_start");

                    var shardRegionPairs = shards.SelectMany(x => controlPlaneInfo.Stamp.DataPlaneLocations, (shard, location) => (shard, location)).Shuffle();

                    // execute with default of three shards at once, 250ms pause between each, with a per-shard lease
                    return taskHelper.RunConcurrentEnumerableAsync(
                        $"{LogBaseName}_{name}_for_each_plan_shard_enumerable",
                        shardRegionPairs,
                        (pair, itemLogger) =>
                        {
                            itemLogger.FluentAddBaseValue(BillingLoggingConstants.Shard, pair.shard);
                            itemLogger.FluentAddBaseValue(BillingLoggingConstants.Location, pair.location);

                            logger.LogInfo($"{LogBaseName}_{name}_for_each_plan_shard_enumerable_item_start");

                            cancellationToken.ThrowIfCancellationRequested();

                            return ExecuteShard(name, action, pair.shard, pair.location, itemLogger, cancellationToken);
                        },
                        childLogger,
                        (pair, itemLogger) =>
                        {
                            return claimedDistributedLease.Obtain(LeaseContainer, $"billing-v2-{name}-{pair.location}-{pair.shard}", TimeSpan.FromMinutes(45), itemLogger);
                        });
                },
                swallowException: true);
        }

        private Task ExecuteShard(string name, Func<VsoPlan, IDiagnosticsLogger, Task> action, string shard, AzureLocation location, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_{name}_execute_shard",
                (childLogger) =>
                {
                    childLogger.LogInfo($"{LogBaseName}_{name}_execute_shard_start");

                    // retrieve plans from Cosmos DB in batches of 100
                    return planManager.GetBillablePlansByShardAsync(
                        shard,
                        location,
                        (plan, childLogger) => Task.CompletedTask, // no per-item work
                        (plans, childLogger) =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            return ExecutePlanBatch(name, action, plans, childLogger, cancellationToken);
                        },
                        logger);
                },
                swallowException: true);
        }

        private Task ExecutePlanBatch(string name, Func<VsoPlan, IDiagnosticsLogger, Task> action, IEnumerable<VsoPlan> plans, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            // concurrently process up to 100 plans (4 at a time by default)
            return taskHelper.RunConcurrentEnumerableAsync(
                $"{LogBaseName}_{name}_execute_plan_batch",
                plans,
                (plan, childLogger) =>
                {
                    return childLogger.OperationScopeAsync(
                        $"{LogBaseName}_{name}_action",
                        async (innerLogger) =>
                        {
                            innerLogger.FluentAddBaseValue(BillingLoggingConstants.PlanId, plan.Id);

                            innerLogger.LogInfo($"{LogBaseName}_{name}_action_start");

                            cancellationToken.ThrowIfCancellationRequested();

                            // lastly, execute the action
                            await action(plan, innerLogger);
                        },
                        swallowException: true);
                },
                logger,
                concurrentLimit: billingSettings.ConcurrentJobConsumerCount,
                successDelay: 0);
        }
    }
}
