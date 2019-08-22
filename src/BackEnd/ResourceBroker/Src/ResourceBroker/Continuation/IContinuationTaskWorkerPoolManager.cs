// <copyright file="IContinuationTaskQueueManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation
{
    public interface IContinuationTaskWorkerPoolManager : IDisposable
    {
        Task StartAsync(IDiagnosticsLogger logger);
    }
}
