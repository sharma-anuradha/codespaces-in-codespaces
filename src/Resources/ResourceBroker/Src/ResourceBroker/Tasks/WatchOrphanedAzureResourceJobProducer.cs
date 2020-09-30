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
    public class WatchOrphanedAzureResourceJobProducer : BaseDataPlaneResourceGroupJobProducer
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

        // Run once every 30 minutes
        protected override (string CronExpression, TimeSpan Interval) ScheduleTimeInterval => JobPayloadRegisterSchedule.WatchOrphanedAzureResourceJobSchedule;

        private ICapacityManager CapacityManager { get; }

        protected override async Task<IEnumerable<IAzureResourceGroup>> RetrieveResourceGroups(IDiagnosticsLogger logger)
        {
            var resourceGroups = await CapacityManager.GetAllDataPlaneResourceGroups(logger);
            return resourceGroups.Shuffle();
        }
    }
}
