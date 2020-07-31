// <copyright file="IJobStartInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts
{
    /// <summary>
    /// Job start info contract.
    /// </summary>
    public interface IJobStartInfo
    {
        /// <summary>
        /// Gets the Date and time of the start.
        /// </summary>
        public DateTime StartTime { get; }

        /// <summary>
        /// Cancel this job.
        /// </summary>
        public void Cancel();
    }
}
