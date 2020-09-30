// <copyright file="DeleteResourceGroupDeploymentsJobProducer.cs" company="Microsoft">
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
    /// Delete resource group deployments job producer
    /// </summary>
    public class DeleteResourceGroupDeploymentsJobProducer : BaseDataPlaneResourceGroupJobProducer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteResourceGroupDeploymentsJobProducer"/> class.
        /// </summary>
        /// /// <param name="capacityManager">The capacity manager instance.</param>
        /// <param name="jobSchedulerFeatureFlags">The job scheduler feature flags instance.</param>
        public DeleteResourceGroupDeploymentsJobProducer(
            ICapacityManager capacityManager,
            IJobSchedulerFeatureFlags jobSchedulerFeatureFlags)
            : base(jobSchedulerFeatureFlags)
        {
            CapacityManager = capacityManager;
        }

        protected override string JobName => "delete_resource_group_deployments_task";

        protected override Type JobHandlerType => typeof(DeleteResourceGroupDeploymentsJobHandler);

        // run every hour
        protected override (string CronExpression, TimeSpan Interval) ScheduleTimeInterval => JobPayloadRegisterSchedule.DeleteResourceGroupDeploymentsJobSchedule;

        private ICapacityManager CapacityManager { get; }

        protected override async Task<IEnumerable<IAzureResourceGroup>> RetrieveResourceGroups(IDiagnosticsLogger logger)
        {
            var resourceGroups = await CapacityManager.GetAllDataPlaneResourceGroups(logger);
            return resourceGroups.Shuffle();
        }
    }
}
