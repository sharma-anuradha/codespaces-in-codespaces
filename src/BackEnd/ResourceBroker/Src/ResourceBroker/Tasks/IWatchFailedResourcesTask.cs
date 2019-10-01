// <copyright file="IWatchFailedResourcesTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Watches for failed resources.
    /// </summary>
    public interface IWatchFailedResourcesTask : IWatchPoolTask
    {
    }
}
