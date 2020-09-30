// <copyright file="BaseDataPlaneResourceGroupJobProducer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Data plane resource group job producer
    /// Payload factory
    /// Job scheduler registration entry point.
    /// </summary>
    public abstract class BaseDataPlaneResourceGroupJobProducer : IJobSchedulerRegister, IJobSchedulePayloadFactory
    {
        /// <summary>
        /// Feature flag to control whether the data plane resource group job is enabled.
        /// </summary>
        public const string DataPlaneResourceGroupJobsEnabledFeatureFlagName = "DataPlaneResourceGroupJobs";

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDataPlaneResourceGroupJobProducer"/> class.
        /// </summary>
        /// <param name="jobSchedulerFeatureFlags">The job scheduler feature flags instance.</param>
        protected BaseDataPlaneResourceGroupJobProducer(
            IJobSchedulerFeatureFlags jobSchedulerFeatureFlags)
        {
            JobSchedulerFeatureFlags = jobSchedulerFeatureFlags;
        }

        protected abstract string JobName { get; }

        protected abstract Type JobHandlerType { get; }

        protected abstract Task<IEnumerable<IAzureResourceGroup>> RetrieveResourceGroups(IDiagnosticsLogger logger);

        protected abstract (string CronExpression, TimeSpan Interval) ScheduleTimeInterval { get; }

        private IJobSchedulerFeatureFlags JobSchedulerFeatureFlags { get; }

        /// <inheritdoc/>
        public void RegisterScheduleJob()
        {
            JobSchedulerFeatureFlags.AddRecurringJobPayload(
                ScheduleTimeInterval.CronExpression,
                $"{JobName}_run",
                ResourceJobQueueConstants.GenericQueueName,
                claimSpan: ScheduleTimeInterval.Interval,
                this,
                DataPlaneResourceGroupJobsEnabledFeatureFlagName);
        }

        /// <inheritdoc/>
        public async Task CreatePayloadsAsync(string jobRunId, DateTime scheduleRun, IServiceProvider serviceProvider, OnPayloadCreatedDelegate onPayloadCreated, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            // Fetch all data plane resource groups
            var dataPlaneResourceGroups = await RetrieveResourceGroups(logger);

            logger.FluentAddValue("TaskCountResourceGroups", dataPlaneResourceGroups.Count().ToString());

            // return produced payloads
            await onPayloadCreated.AddAllPayloadsAsync(dataPlaneResourceGroups, (resourceGroup) =>
            {
                var jobPayload = (ResourceGroupPayloadBase)Activator.CreateInstance(typeof(ResourceGroupPayload<>).MakeGenericType(JobHandlerType));
                jobPayload.SubscriptionId = resourceGroup.Subscription.SubscriptionId;
                jobPayload.ResourceGroupName = resourceGroup.ResourceGroup;
                return jobPayload;
            });
        }

        /// <summary>
        /// A resource group payload.
        /// </summary>
        /// <typeparam name="T">Type of the job handler.</typeparam>
        public class ResourceGroupPayload<T> : ResourceGroupPayloadBase
            where T : class
        {
        }

        /// <summary>
        /// A resource group payload base class.
        /// </summary>
        /// <typeparam name="T">Type of the job handler.</typeparam>
        public class ResourceGroupPayloadBase : JobPayload
        {
            /// <summary>
            /// Gets or sets the subscription id.
            /// </summary>
            public string SubscriptionId { get; set; }

            /// <summary>
            /// Gets or sets the resource group name.
            /// </summary>
            public string ResourceGroupName { get; set; }
        }
    }
}
