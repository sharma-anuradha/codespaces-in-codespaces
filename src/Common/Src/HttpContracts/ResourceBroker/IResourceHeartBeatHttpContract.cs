// <copyright file="IResourceHeartBeatHttpContract.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker
{
    /// <summary>
    /// Resource HeartBeat Http Contract.
    /// </summary>
    public interface IResourceHeartBeatHttpContract
    {
        /// <summary>
        /// Update heartbeat for a VM.
        /// </summary>
        /// <param name="resourceId">VM Resource Id.</param>
        /// <param name="heartBeat">Heartbeat message.</param>
        /// <param name="logger">IDiagnostics Logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task UpdateHeartBeatAsync(Guid resourceId, HeartBeatBody heartBeat, IDiagnosticsLogger logger);
    }
}
