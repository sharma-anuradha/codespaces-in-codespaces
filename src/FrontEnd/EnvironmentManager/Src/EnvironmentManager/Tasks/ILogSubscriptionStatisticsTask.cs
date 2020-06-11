// <copyright file="ILogSubscriptionStatisticsTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks
{
    /// <summary>
    /// Task which logs information about plans and subscriptions.
    /// </summary>
    public interface ILogSubscriptionStatisticsTask
    {
        /// <summary>
        /// Task which continually logs information about the state of the cloud Environments collection.
        /// </summary>
        /// <param name="claimSpan">Target claim span.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Whether the task should run again.</returns>
        Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger);
    }
}
