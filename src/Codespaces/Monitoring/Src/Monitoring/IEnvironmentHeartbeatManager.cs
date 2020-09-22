// <copyright file="IEnvironmentHeartbeatManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Monitoring
{
    public interface IEnvironmentHeartbeatManager
    {
        /// <summary>
        /// Process heartbeat data.
        /// </summary>
        /// <param name="heartBeat">Heartbeat data.</param>
        /// <param name="environment">environment.</param>
        /// <param name="logger">logger</param>
        /// <returns>result.</returns>
        Task<IEnumerable<Exception>> ProcessCollectedDataAsync(
            HeartBeatBody heartBeat,
            CloudEnvironment environment,
            IDiagnosticsLogger logger);
    }
}