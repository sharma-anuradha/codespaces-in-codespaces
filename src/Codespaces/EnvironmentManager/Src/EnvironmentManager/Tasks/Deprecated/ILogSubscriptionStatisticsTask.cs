// <copyright file="ILogSubscriptionStatisticsTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks
{
    /// <summary>
    /// Task which logs information about plans and subscriptions.
    /// </summary>
    public interface ILogSubscriptionStatisticsTask : IBackgroundTask
    {
    }
}
