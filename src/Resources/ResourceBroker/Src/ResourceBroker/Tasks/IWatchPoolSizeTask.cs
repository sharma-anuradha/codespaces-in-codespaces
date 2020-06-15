// <copyright file="IWatchPoolSizeTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Task mananager that watches the pool size and determins if any delta operations need to be
    /// performed to fill/drain the pool.
    /// </summary>
    public interface IWatchPoolSizeTask : IBackgroundTask
    {
    }
}
