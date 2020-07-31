// <copyright file="IJobEndInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts
{
    /// <summary>
    /// Job Start Info contract.
    /// </summary>
    public interface IJobEndInfo
    {
        /// <summary>
        /// Gets the Date and time of the start.
        /// </summary>
        public DateTime StartTime { get; }

        /// <summary>
        /// Gets the elapsed time of the job.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Gets the Job's exception.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets the Date and time of next run.
        /// </summary>
        public DateTime? NextRun { get; }
    }
}
