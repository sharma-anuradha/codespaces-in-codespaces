// <copyright file="IBannedSubscriptionTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Subscriptions
{
    /// <summary>
    /// Represents the task that will look for banned subscriptions and mark their plans for deletion.
    /// </summary>
    public interface IBannedSubscriptionTask : IBackgroundTask
    {
    }
}
