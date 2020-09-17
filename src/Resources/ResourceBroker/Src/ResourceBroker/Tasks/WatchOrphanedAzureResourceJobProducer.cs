// <copyright file="WatchOrphanedAzureResourceJobProducer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Watch orphaned Azure resource job producer
    /// </summary>
    public class WatchOrphanedAzureResourceJobProducer : DataPlaneResourceGroupJobProducer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchOrphanedAzureResourceJobProducer"/> class.
        /// </summary>
        /// /// <param name="capacityManager">The capacity manager instance.</param>
        /// <param name="jobSchedulerFeatureFlags">The job scheduler feature flags instance.</param>
        public WatchOrphanedAzureResourceJobProducer(
            ICapacityManager capacityManager,
            IJobSchedulerFeatureFlags jobSchedulerFeatureFlags)
            : base(jobSchedulerFeatureFlags)
        {
            CapacityManager = capacityManager;
        }

        protected override string JobName => "watch_orphaned_azure_resource_task";

        protected override Type JobHandlerType => typeof(WatchOrphanedAzureResourceJobHandler);

        protected override (string CronExpression, TimeSpan Interval) ScheduleTimeInterval => ("*/30 * * * *", TimeSpan.FromMinutes(30));

        private ICapacityManager CapacityManager { get; }

        protected override async Task<IEnumerable<IAzureResourceGroup>> RetrieveResourceGroups(IDiagnosticsLogger logger)
        {
            var resourceGroups = await CapacityManager.GetAllDataPlaneResourceGroups(logger);
            return resourceGroups.Shuffle();
        }
    }
}
