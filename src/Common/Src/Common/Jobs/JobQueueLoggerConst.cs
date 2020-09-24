// <copyright file="JobQueueLoggerConst.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs
{
    /// <summary>
    /// Logger constants for the Job Queue telemetry.
    /// </summary>
    public static class JobQueueLoggerConst
    {
        /// <summary>
        /// The job id column.
        /// </summary>
        public const string JobId = "JobId";

        /// <summary>
        /// The job type column.
        /// </summary>
        public const string JobType = "JobType";

        /// <summary>
        /// The job payload column.
        /// </summary>
        public const string JobPayload = "JobPayload";

        /// <summary>
        /// The job initial visbility delay.
        /// </summary>
        public const string InitialVisibilityDelay = "InitialVisibilityDelay";

        /// <summary>
        /// The job expiration timeout.
        /// </summary>
        public const string ExpireTimeout = "ExpireTimeout";

        /// <summary>
        /// Job queue duration.
        /// </summary>
        public const string JobQueueDuration = "JobQueueDuration";

        /// <summary>
        /// Job dequeue duration.
        /// </summary>
        public const string JobDequeuedDuration = "JobDequeuedDuration";

        /// <summary>
        /// Job handler duration.
        /// </summary>
        public const string JobHandlerDuration = "JobHandlerDuration";

        /// <summary>
        /// Total job duration.
        /// </summary>
        public const string JobDuration = "JobDuration";

        /// <summary>
        /// If job was expired.
        /// </summary>
        public const string JobDidExpired = "JobDidExpired";

        /// <summary>
        /// If job was cancelled.
        /// </summary>
        public const string JobDidCancel = "JobDidCancel";

        /// <summary>
        /// Number of retries for this job.
        /// </summary>
        public const string JobRetries = "JobRetries";

        /// <summary>
        /// Job queue minimum input count.
        /// </summary>
        public const string JobQueueMinInputCount = "JobQueueMinInputCount";

        /// <summary>
        /// Job queue maximum input count.
        /// </summary>
        public const string JobQueueMaxInputCount = "JobQueueMaxInputCount";

        /// <summary>
        /// Number of jobs processed.
        /// </summary>
        public const string JobProcessedCount = "JobProcessedCount";

        /// <summary>
        /// Job average process time.
        /// </summary>
        public const string JobAverageProcessTime = "JobAverageProcessTime";

        /// <summary>
        /// Job average dequeud duration.
        /// </summary>
        public const string JobAverageDequeuedDuration = "JobAverageDequeuedDuration";

        /// <summary>
        /// Jobs that failed.
        /// </summary>
        public const string JobFailuresCount = "JobFailuresCount";

        /// <summary>
        /// Jobs retries.
        /// </summary>
        public const string JobRetriesCount = "JobRetriesCount";

        /// <summary>
        /// Jobs cancelled.
        /// </summary>
        public const string JobCancelledCount = "JobCancelledCount";

        /// <summary>
        /// Jobs that expired.
        /// </summary>
        public const string JobExpiredCount = "JobExpiredCount";

        /// <summary>
        /// Percentile 50 of process time.
        /// </summary>
        public const string JobPercentile50Time = "JobPercentile50Time";

        /// <summary>
        /// Percentile 90 of process time.
        /// </summary>
        public const string JobPercentile90Time = "JobPercentile90Time";

        /// <summary>
        /// Percentile 99 of process time.
        /// </summary>
        public const string JobPercentile99Time = "JobPercentile99Time";

        /// <summary>
        /// Keep invisible count.
        /// </summary>
        public const string JobKeepInvisibleCount = "JobKeepInvisibleCount";
    }
}
