// <copyright file="EnvironmentStats.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UsageAnalytics
{
    /// <summary>
    /// Usage stats of an environment.
    /// </summary>
    public class EnvironmentStats
    {
        /// <summary>
        /// Gets or sets the creation time of an environment.
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Gets or sets the total active time of an environment.
        /// </summary>
        public double TotalTimeActive { get; set; }

        /// <summary>
        /// Gets or sets the total suspension time of an environment.
        /// </summary>
        public double TotalTimeSuspend { get; set; }

        /// <summary>
        /// Gets or sets the number of times an environment was shutdown.
        /// </summary>
        public int NumberOfTimeShutdown { get; set; }

        /// <summary>
        /// Gets or sets the number of times an environment was active.
        /// </summary>
        public int NumberOfTimesActive { get; set; }

        /// <summary>
        /// Gets or sets the average session time .
        /// </summary>
        public double AverageTimeToShutdown { get; set; }

        /// <summary>
        /// Gets or sets the average time an environment had to wait to be active again after shutdown.
        /// </summary>
        public double AverageTimeToNextUse { get; set; }

        /// <summary>
        /// Gets or sets the max span of time an environment had to wait to be active again after shutdown.
        /// </summary>
        public double MaxTimeToNextUse { get; set; }
    }
}