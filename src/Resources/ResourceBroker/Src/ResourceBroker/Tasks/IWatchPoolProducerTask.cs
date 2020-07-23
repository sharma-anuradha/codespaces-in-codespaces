// <copyright file="IWatchPoolProducerTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Task mananager that tries to tick off a continuation which will try and manage tracking
    /// of the "current" version and conduct orchistrate drains as requried.
    /// </summary>
    public interface IWatchPoolProducerTask : IBackgroundTask
    {
    }
}
