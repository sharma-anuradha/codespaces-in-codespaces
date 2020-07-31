// <copyright file="JobSchedulerLoggerConst.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler
{
    /// <summary>
    /// Logger constants for the Job Queue telemetry.
    /// </summary>
    internal static class JobSchedulerLoggerConst
    {
        /// <summary>
        /// The job name column.
        /// </summary>
        public const string JobName = "JobName";

        /// <summary>
        /// The job schedule run date time.
        /// </summary>
        public const string JobScheduleRun = "JobScheduleRun";

        /// <summary>
        /// The job run identifier.
        /// </summary>
        public const string JobRunId = "JobRunId";
    }
}
