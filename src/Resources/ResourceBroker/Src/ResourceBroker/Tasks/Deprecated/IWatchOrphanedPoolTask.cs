// <copyright file="IWatchOrphanedPoolTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Task mananager that tries to kick off a continuation which will try and manage tracking
    /// orphaned pools and conduct orchestrate drains as requried.
    /// </summary>
    public interface IWatchOrphanedPoolTask : IBackgroundTask
    {
    }
}
