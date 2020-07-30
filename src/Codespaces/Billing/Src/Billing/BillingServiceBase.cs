// <copyright file="BillingServiceBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Contains base implentations for all billing services.
    /// </summary>
    public abstract class BillingServiceBase
    {
        /// <summary>
        /// According to the FAQs - usage meters should arrive within 48 hours of when it was incurred.
        /// https://microsoft.sharepoint.com/teams/CustomerAcquisitionBilling/_layouts/15/WopiFrame.aspx?sourcedoc={c7a559f4-316d-46b1-b5d4-f52cdfbc4389}&action=edit&wd=target%28Onboarding.one%7C55f62e8d-ea91-4a90-982c-04899e106633%2FFAQ%7C25cc6e79-1e39-424d-9403-cd05d2f675e9%2F%29&wdorigin=703.
        /// </summary>
        private readonly int lookBackThresholdHrs = 48;
        private readonly IControlPlaneInfo controlPlaneInfo;
        private readonly IClaimedDistributedLease claimedDistributedLease;
        private readonly ITaskHelper taskHelper;
        private readonly IPlanManager planManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingServiceBase"/> class.
        /// </summary>
        /// <param name="controlPlaneInfo">Control plane info used to figure out which azure regions we're operating on.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="claimedDistributedLease">Used to create leases for the workers.</param>
        /// <param name="taskHelper">Used to paralellize the work.</param>
        /// <param name="planManager">Plan manager used to get a list of plans to bill/submit.</param>
        /// <param name="serviceName">the service name used to set up the logger.</param>
        public BillingServiceBase(
            IControlPlaneInfo controlPlaneInfo,
            IDiagnosticsLogger logger,
            IClaimedDistributedLease claimedDistributedLease,
            ITaskHelper taskHelper,
            IPlanManager planManager)
        {
            this.controlPlaneInfo = controlPlaneInfo;
            Logger = logger.NewChildLogger();
            this.claimedDistributedLease = claimedDistributedLease;
            this.taskHelper = taskHelper;
            this.planManager = planManager;
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        protected IDiagnosticsLogger Logger { get; private set; }

        /// <summary>
        /// Gets the service named used for logging.
        /// </summary>
        protected abstract string ServiceName { get; }

        /// <summary>
        /// The outer execute method for any service.
        /// </summary>
        /// <param name="token">the cancellation token.</param>
        /// <returns>a task indicating completion of the method.</returns>
        public async Task Execute(CancellationToken token)
        {
            // TODO: consider on the hour billing summary creation. For example summarys would have a time range of
            // 12:00:00 -> 1:00:00
            var now = DateTime.UtcNow;
            var absoluteDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
            var start = absoluteDate.Subtract(TimeSpan.FromHours(this.lookBackThresholdHrs));
            var end = absoluteDate;
            var controlPlaneRegions = this.controlPlaneInfo.Stamp.DataPlaneLocations.Shuffle();
            var planShards = GetShards();
            var plansToRegionsShards = planShards.SelectMany(x => controlPlaneRegions, (planShard, region) => new { planShard, region });

            Logger.FluentAddValue("startCalculationTime", start);
            Logger.FluentAddValue("endCalculationTime", end);

            await this.taskHelper.RunConcurrentEnumerableAsync(
                $"{ServiceName}_run",
                plansToRegionsShards,
                async (x, childlogger) =>
                {
                    var planShard = x.planShard;
                    var region = x.region;
                    var leaseName = $"{ServiceName}-{planShard}-{region}".ToLowerInvariant();

                    // leaseName += Guid.NewGuid().ToString();
                    childlogger.FluentAddBaseValue("Service", "billingservices");
                    using (var lease = await this.claimedDistributedLease.Obtain(
                                                  $"{ServiceName}-leases",
                                                  leaseName,
                                                  TimeSpan.FromHours(1),
                                                  childlogger))
                    {
                        if (lease != null)
                        {
                            await ExecuteInner(childlogger, start, end, planShard, region);
                        }
                    }
                },
                Logger);
        }

        /// <summary>
        /// Get the shards for the plan query.
        /// </summary>
        /// <returns>Returns a list of single character strings.</returns>
        protected virtual IEnumerable<string> GetShards()
        {
            return this.planManager.GetShards();
        }

        /// <summary>
        /// Executes the inner method for the given service.
        /// </summary>
        /// <param name="childlogger">The logger that should be used.</param>
        /// <param name="start">the start time for the billing service's current cycle.</param>
        /// <param name="end">the end time for the bill to be generated/processed.</param>
        /// <param name="planShard">the specific plan shard.</param>
        /// <param name="region">the region being operated in.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected abstract Task ExecuteInner(IDiagnosticsLogger childlogger, DateTime start, DateTime end, string planShard, AzureLocation region);
    }
}
