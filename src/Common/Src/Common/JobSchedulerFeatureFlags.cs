// <copyright file="JobSchedulerFeatureFlags.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Implements IJobSchedulerFeatureFlags.
    /// </summary>
    public class JobSchedulerFeatureFlags : IJobSchedulerFeatureFlags
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobSchedulerFeatureFlags"/> class.
        /// </summary>
        /// <param name="jobSchedulerLease">A job scheduler lease instance.</param>
        /// <param name="systemConfiguration">A system configuration instance.</param>
        /// <param name="logger">Logger instance.</param>
        public JobSchedulerFeatureFlags(
            IJobSchedulerLease jobSchedulerLease,
            ISystemConfiguration systemConfiguration,
            IDiagnosticsLogger logger)
        {
            JobSchedulerLease = Requires.NotNull(jobSchedulerLease, nameof(jobSchedulerLease));
            SystemConfiguration = Requires.NotNull(systemConfiguration, nameof(jobSchedulerLease));
            Logger = Requires.NotNull(logger, nameof(logger));
        }

        private IJobSchedulerLease JobSchedulerLease { get; }

        private ISystemConfiguration SystemConfiguration { get; }

        private IDiagnosticsLogger Logger { get; }

        /// <inheritdoc/>
        public IJobSchedulePayloadFactory CreateFeatureFlagsPayloadFactory(IJobSchedulePayloadFactory jobSchedulePayloadFactory, string featureFlagName)
        {
            Requires.NotNull(jobSchedulePayloadFactory, nameof(jobSchedulePayloadFactory));
            Requires.NotNullOrEmpty(featureFlagName, nameof(featureFlagName));

            return new FeatureFlagsPayloadFactory(this, jobSchedulePayloadFactory, featureFlagName);
        }

        /// <inheritdoc/>
        public Task<bool> IsFeatureFlagEnabledAsync(string featureFlagName, bool defaultValue = false)
        {
            return SystemConfiguration.GetValueAsync($"featureflag:{featureFlagName}", Logger, defaultValue);
        }

        /// <inheritdoc/>
        public IScheduleJob AddRecurringJobPayload(
            string expression,
            string jobName,
            string queueName,
            TimeSpan claimSpan,
            IJobSchedulePayloadFactory jobSchedulePayloadFactory,
            string featureFlagName)
        {
            return JobSchedulerLease.AddRecurringJobPayload(
                expression,
                jobName,
                queueName,
                claimSpan,
                CreateFeatureFlagsPayloadFactory(jobSchedulePayloadFactory, featureFlagName));
        }

        private class FeatureFlagsPayloadFactory : IJobSchedulePayloadFactory
        {
            private readonly JobSchedulerFeatureFlags jobSchedulerFeatureFlags;
            private readonly IJobSchedulePayloadFactory jobSchedulePayloadFactory;
            private readonly string featureFlagName;

            public FeatureFlagsPayloadFactory(JobSchedulerFeatureFlags jobSchedulerFeatureFlags, IJobSchedulePayloadFactory jobSchedulePayloadFactory, string featureFlagName)
            {
                this.jobSchedulerFeatureFlags = jobSchedulerFeatureFlags;
                this.jobSchedulePayloadFactory = jobSchedulePayloadFactory;
                this.featureFlagName = featureFlagName;
            }

            public async Task<IEnumerable<(JobPayload, JobPayloadOptions)>> CreatePayloadsAsync(string jobRunId, DateTime scheduleRun, IServiceProvider serviceProvider, IDiagnosticsLogger logger, CancellationToken cancellationToken)
            {
                if (await jobSchedulerFeatureFlags.IsFeatureFlagEnabledAsync(this.featureFlagName))
                {
                    return await this.jobSchedulePayloadFactory.CreatePayloadsAsync(jobRunId, scheduleRun, serviceProvider, logger, cancellationToken);
                }

                return Array.Empty<(JobPayload, JobPayloadOptions)>();
            }
        }
    }
}
