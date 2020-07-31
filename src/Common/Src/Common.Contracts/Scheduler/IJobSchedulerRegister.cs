// <copyright file="IJobSchedulerRegister.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts
{
    /// <summary>
    /// Interface to define an entity that would need to register a job scheduler.
    /// </summary>
    public interface IJobSchedulerRegister
    {
        /// <summary>
        /// Register this job on the scheduler.
        /// </summary>
        void RegisterScheduleJob();
    }
}
