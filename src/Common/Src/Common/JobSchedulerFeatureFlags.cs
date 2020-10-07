// <copyright file="JobSchedulerFeatureFlags.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
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
        /// <param name="configurationReader">A configuration reader instance.</param>
        /// <param name="logger">Logger instance.</param>
        public JobSchedulerFeatureFlags(
            IJobSchedulerLease jobSchedulerLease,
            IConfigurationReader configurationReader,
            IDiagnosticsLogger logger)
        {
            JobSchedulerLease = Requires.NotNull(jobSchedulerLease, nameof(jobSchedulerLease));
            ConfigurationReader = Requires.NotNull(configurationReader, nameof(configurationReader));
            Logger = Requires.NotNull(logger, nameof(logger));
        }

        private IJobSchedulerLease JobSchedulerLease { get; }

        private IConfigurationReader ConfigurationReader { get; }

        private IDiagnosticsLogger Logger { get; }

        /// <inheritdoc/>
        public Task<bool> IsFeatureFlagEnabledAsync(string featureFlagName, bool defaultValue = false)
        {
            return ConfigurationReader.ReadSettingAsync(featureFlagName, ConfigurationConstants.EnabledSettingName, Logger, defaultValue);
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
                jobSchedulePayloadFactory,
                (dt) => string.IsNullOrEmpty(featureFlagName) ? Task.FromResult(true) : IsFeatureFlagEnabledAsync(featureFlagName, true));
        }
    }
}
