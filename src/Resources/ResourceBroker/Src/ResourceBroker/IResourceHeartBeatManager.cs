// <copyright file="IResourceHeartBeatManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Manages heartbeat messages.
    /// </summary>
    public interface IResourceHeartBeatManager
    {
        /// <summary>
        /// Recieve HeartBeat.
        /// </summary>
        /// <param name="heartBeatInput">HeartBeat input.</param>
        /// <param name="logger">IDiagnostics Logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task SaveHeartBeatAsync(HeartBeatInput heartBeatInput, IDiagnosticsLogger logger);
    }
}