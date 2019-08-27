// <copyright file="IContinuationTaskQueueManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation
{
    /// <summary>
    /// 
    /// </summary>
    public interface IContinuationTaskWorkerPoolManager : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        int CurrentWorkerCount { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        Task StartAsync(IDiagnosticsLogger logger);
    }
}
