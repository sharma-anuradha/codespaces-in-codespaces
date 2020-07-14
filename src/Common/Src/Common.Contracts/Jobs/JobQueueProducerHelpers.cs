// <copyright file="JobQueueProducerHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts.Jobs
{
    /// <summary>
    /// Helper extension for a job queue producer.
    /// </summary>
    public static class JobQueueProducerHelpers
    {
        /// <summary>
        /// Add a job with an invisible delay.
        /// </summary>
        /// <param name="jobQueueProducer">The job queue producer.</param>
        /// <param name="jobPayload">The job payload instance.</param>
        /// <param name="initialVisibilityDelay">Initial visibility delay.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Completion task.</returns>
        public static Task AddJobWithDelayAsync(this IJobQueueProducer jobQueueProducer, JobPayload jobPayload, TimeSpan initialVisibilityDelay, CancellationToken cancellationToken)
        {
            Requires.NotNull(jobQueueProducer, nameof(jobQueueProducer));

            return jobQueueProducer.AddJobAsync(jobPayload, new JobPayloadOptions() { InitialVisibilityDelay = initialVisibilityDelay }, cancellationToken);
        }

        /// <summary>
        /// Add a job using a callback target time callback.
        /// </summary>
        /// <param name="jobQueueProducer">The job queue producer.</param>
        /// <param name="jobPayload">The job payload instance.</param>
        /// <param name="targetTimeCallback">The callback to get the target time.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Completion task.</returns>
        public static Task AddJobWithTargetAsync(this IJobQueueProducer jobQueueProducer, JobPayload jobPayload, Func<DateTime, DateTime> targetTimeCallback, CancellationToken cancellationToken)
        {
            Requires.NotNull(targetTimeCallback, nameof(targetTimeCallback));

            var currentTime = DateTime.UtcNow;
            var targetTime = targetTimeCallback(currentTime);
            var initialVisibilityDelay = targetTime - currentTime;
            return AddJobWithDelayAsync(jobQueueProducer, jobPayload, initialVisibilityDelay > TimeSpan.Zero ? initialVisibilityDelay : TimeSpan.Zero, cancellationToken);
        }

        /// <summary>
        /// Add a job with a specific target time.
        /// </summary>
        /// <param name="jobQueueProducer">The job queue producer.</param>
        /// <param name="jobPayload">The job payload instance.</param>
        /// <param name="targetTime">The target time until the item is invisible.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Completion task.</returns>
        public static Task AddJobWithTargetAsync(this IJobQueueProducer jobQueueProducer, JobPayload jobPayload, DateTime targetTime, CancellationToken cancellationToken)
        {
            return AddJobWithTargetAsync(jobQueueProducer, jobPayload, (now) => targetTime, cancellationToken);
        }
    }
}
