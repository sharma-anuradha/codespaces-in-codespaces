// <copyright file="IContinuationTaskWorker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation
{
    public interface IContinuationTaskWorker : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether this worker is busy. Used as a proxy to
        /// determine whether it could be cleanned up.
        /// </summary>
        int ActivityLevel { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        Task<bool> Run(IDiagnosticsLogger logger);
    }
}
