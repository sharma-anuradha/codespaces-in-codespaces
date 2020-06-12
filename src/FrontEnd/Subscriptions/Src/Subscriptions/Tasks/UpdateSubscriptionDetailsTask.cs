// <copyright file="UpdateSubscriptionDetailsTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Subscriptions
{
    /// <summary>
    /// Updates subscription details.
    /// </summary>
    public class UpdateSubscriptionDetailsTask : IUpdateSubscriptionDetailsTask
    {
        // Add an artificial delay between DB queries so that we reduce bursty load on our database to prevent throttling for end users
        private static readonly TimeSpan QueryDelay = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateSubscriptionDetailsTask"/> class.
        /// </summary>
        /// <param name="environmentManagerSettings">Used for lease container name.</param>
        /// <param name="cloudEnvironmentRepository">Used for all environment manager sub queries.</param>
        /// <param name="taskHelper">the task helper.</param>
        /// <param name="claimedDistributedLease"> used to create leases.</param>
        /// <param name="resourceNameBuilder">Used to build the lease name.</param>
        /// <param name="subscriptionManager">the subscription manager.</param>
        /// <param name="subscriptionRepository">The subscription repository.</param>
        /// <param name="planRepository">The plan repository.</param>
        /// <param name="controlPlanInfo">Control plane info.</param>
        public UpdateSubscriptionDetailsTask(
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder,
            ISubscriptionManager subscriptionManager,
            IPlanRepository planRepository)
        {
            TaskHelper = taskHelper;
            ClaimedDistributedLease = claimedDistributedLease;
            ResourceNameBuilder = resourceNameBuilder;
            SubscriptionManager = subscriptionManager;
            PlanRepository = planRepository;
        }

        private string LogBaseName => "update-subscription_details";

        private string LeaseBaseName => "update-subscription-details";

        private ITaskHelper TaskHelper { get; }

        private IClaimedDistributedLease ClaimedDistributedLease { get; }

        private IResourceNameBuilder ResourceNameBuilder { get; }

        private ISubscriptionManager SubscriptionManager { get; }

        private IPlanRepository PlanRepository { get; }

        private bool Disposed { get; set; }

        /// <inheritdoc />
        public Task<bool> RunAsync(TimeSpan taskInterval, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                 $"{LogBaseName}_run",
                 async (childLogger) =>
                 {
                     // Basic shard by starting resource id character
                     // NOTE: If over time we needed an additional dimention, we could add region
                     //       and do a cross product with it.
                     var idShards = new List<string> { "a", "b", "c", "d", "e", "f", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }.Shuffle();

                     // Run through found resources in the background
                     await TaskHelper.RunConcurrentEnumerableAsync(
                          $"{LogBaseName}_run_unit_check",
                          idShards,
                          (idShard, itemLogger) => CoreRunUnitAsync(idShard, itemLogger),
                          childLogger,
                          (idShard, itemLogger) => ObtainLeaseAsync($"{LeaseBaseName}-{idShard}1", taskInterval, itemLogger));

                     return !Disposed;
                 },
                 (e, childLogger) => Task.FromResult(!Disposed),
                 swallowException: true);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Disposed = true;
        }

        private Task CoreRunUnitAsync(string idShard, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("TaskResourceIdShard", idShard);

            // Get record so we can tell if it exists
            return PlanRepository.ForEachAsync(
                x => x.Id.StartsWith(idShard),
                logger.NewChildLogger(),
                (plan, innerLogger) =>
                {
                    // Log each item
                    return innerLogger.OperationScopeAsync(
                        $"{LogBaseName}_process_record",
                        async (childLogger)
                        =>
                        {
                            var subscription = await SubscriptionManager.GetSubscriptionAsync(plan.Plan.Subscription, childLogger.NewChildLogger());
                            var subscriptionDetails = await SubscriptionManager.GetSubscriptionDetailsFromExternalSourceAsync(subscription, childLogger.NewChildLogger());
                            if (subscriptionDetails != null)
                            {
                                if (!Enum.TryParse(subscriptionDetails.State, true, out SubscriptionStateEnum subscriptionStateEnum))
                                {
                                    logger.AddValue("SubscriptionState", subscriptionDetails.State);
                                    logger.LogErrorWithDetail("subscription_state_error", $"Subscription state could not be parsed.");
                                }
                                else
                                {
                                    // Update the subscription state
                                    subscription = await SubscriptionManager.UpdateSubscriptionStateAsync(subscription, subscriptionStateEnum, childLogger.NewChildLogger());

                                    // update the subscription offer.
                                    subscription = await SubscriptionManager.UpdateSubscriptionQuotaAsync(subscription, subscriptionDetails.QuotaId, childLogger.NewChildLogger());
                                }
                            }

                            // Pause to rate limit ourselves
                            await Task.Delay(QueryDelay);
                        });
                },
                (_, __) => Task.Delay(QueryDelay));
        }

        private Task<IDisposable> ObtainLeaseAsync(string leaseName, TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return ClaimedDistributedLease.Obtain(
                "subscription_leases", leaseName, claimSpan, logger);
        }
    }
}
