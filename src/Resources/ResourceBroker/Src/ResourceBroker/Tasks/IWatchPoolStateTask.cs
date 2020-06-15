// <copyright file="IWatchPoolStateTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Task mananager that takes regular snapshot of the state of the resource pool.
    /// </summary>
    public interface IWatchPoolStateTask : IBackgroundTask
    {
    }
}
