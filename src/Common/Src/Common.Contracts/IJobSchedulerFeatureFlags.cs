// <copyright file="IJobSchedulerFeatureFlags.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// A job scheduler with feature flags.
    /// </summary>
    public interface IJobSchedulerFeatureFlags
    {
        /// <summary>
        /// Return if a feature flag name is enabled.
        /// </summary>
        /// <param name="featureFlagName">The feature flag name.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>Completion task.</returns>
        Task<bool> IsFeatureFlagEnabledAsync(string featureFlagName, bool defaultValue = false);

        /// <summary>
        /// Add a recurring job payload.
        /// </summary>
        /// <param name="expression">Cron expression to be passed.</param>
        /// <param name="jobName">The job name.</param>
        /// <param name="queueName">A queue name.</param>
        /// <param name="claimSpan">Claim span to pass.</param>
        /// <param name="jobSchedulePayloadFactory">The original job schedule payload factory to wrap.</param>
        /// <param name="featureFlagName">The feature flag name to use.</param>
        /// <param name="isDefaultEnabled">Is scheduled job default enabled</param>
        /// <returns>The job schedule instance.</returns>
        IScheduleJob AddRecurringJobPayload(
            string expression,
            string jobName,
            string queueName,
            TimeSpan claimSpan,
            IJobSchedulePayloadFactory jobSchedulePayloadFactory,
            string featureFlagName,
            bool isDefaultEnabled = true);
    }
}
