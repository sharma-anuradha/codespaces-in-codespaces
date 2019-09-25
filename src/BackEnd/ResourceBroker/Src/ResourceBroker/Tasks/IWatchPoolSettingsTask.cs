// <copyright file="IWatchPoolSettingsTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Task mananager that regularly checks if pool settings have been updated.
    /// </summary>
    public interface IWatchPoolSettingsTask : IDisposable
    {
        /// <summary>
        /// Core task which runs to check which resources are in a bad state.
        /// </summary>
        /// <param name="rootLogger">Target logger.</param>
        /// <returns>Whether the task should run again.</returns>
        Task<bool> RunAsync(IDiagnosticsLogger rootLogger);
    }
}
