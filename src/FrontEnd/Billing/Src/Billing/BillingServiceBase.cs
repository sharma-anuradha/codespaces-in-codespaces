// <copyright file="BillingServiceBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    public abstract class BillingServiceBase
    {

        /// <summary>
        /// According to the FAQs - usage meters should arrive within 48 hours of when it was incurred.
        /// https://microsoft.sharepoint.com/teams/CustomerAcquisitionBilling/_layouts/15/WopiFrame.aspx?sourcedoc={c7a559f4-316d-46b1-b5d4-f52cdfbc4389}&action=edit&wd=target%28Onboarding.one%7C55f62e8d-ea91-4a90-982c-04899e106633%2FFAQ%7C25cc6e79-1e39-424d-9403-cd05d2f675e9%2F%29&wdorigin=703
        /// </summary>
        private readonly int lookBackThresholdHrs = 48;

        private readonly IBillingEventManager billingEventManager;
        private readonly IControlPlaneInfo controlPlaneInfo;
        private readonly IClaimedDistributedLease claimedDistributedLease;
        private readonly ITaskHelper taskHelper;

        protected IDiagnosticsLogger Logger;

        public BillingServiceBase( 
            IBillingEventManager billingEventManager,
            IControlPlaneInfo controlPlaneInfo,
            IDiagnosticsLogger logger,
            IClaimedDistributedLease claimedDistributedLease,
            ITaskHelper taskHelper,
            string serviceName)
        {
            this.billingEventManager = billingEventManager;
            this.controlPlaneInfo = controlPlaneInfo;
            this.Logger = logger.NewChildLogger();
            this.claimedDistributedLease = claimedDistributedLease;
            this.taskHelper = taskHelper;

            ServiceName = serviceName;
        }

        /// <summary>
        /// Gets the service named used for logging
        /// </summary>
        protected string ServiceName { get; }

        /// <summary>
        /// The outer execute method for any service.
        /// </summary>
        /// <param name="token">the cancellation token</param>
        /// <returns>a task indicating completion of the method</returns>
        public async Task Execute(CancellationToken token)
        {
            // TODO: consider on the hour billing summary creation. For example summarys would have a time range of
            // 12:00:00 -> 1:00:00
            var absoluteDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0, DateTimeKind.Utc);
            var start = absoluteDate.Subtract(TimeSpan.FromHours(lookBackThresholdHrs));
            var end = absoluteDate;
            var controlPlaneRegions = controlPlaneInfo.GetAllDataPlaneLocations().Shuffle();
            var planShards = billingEventManager.GetShards();
            var plansToRegionsShards = planShards.SelectMany(x => controlPlaneRegions, (planShard, region) => new { planShard, region });

            Logger.FluentAddValue("startCalculationTime", start);
            Logger.FluentAddValue("endCalculationTime", end);

            await taskHelper.RunBackgroundEnumerableAsync(
                $"{ServiceName}-run",
                plansToRegionsShards,
                async (x, childlogger) =>
                {
                    var planShard = x.planShard;
                    var region = x.region;
                    var leaseName = $"{ServiceName}-{planShard}-{region}";
                    childlogger.FluentAddBaseValue("Service", "billingservices");
                    childlogger.FluentAddBaseValue("leaseName", leaseName);
                    using (var lease = await claimedDistributedLease.Obtain(
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

        protected abstract Task ExecuteInner(IDiagnosticsLogger childlogger, DateTime start, DateTime end, string planShard, AzureLocation region);

    }
}
