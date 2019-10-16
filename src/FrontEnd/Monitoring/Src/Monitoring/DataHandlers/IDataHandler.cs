// <copyright file="IDataHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Monitoring.DataHandlers
{
    /// <summary>
    /// Data Handler interface.
    /// </summary>
    public interface IDataHandler
    {
        /// <summary>
        /// Check if the state can be processed by the handler.
        /// </summary>
        /// <param name="data">Data collected by monitors, job results etc.</param>
        /// <returns>True if the handler can process the collected data.</returns>
        bool CanProcess(CollectedData data);

        /// <summary>
        /// Specific handling for the given monitor state.
        /// </summary>
        /// <param name="data">Data collected by monitors, job results etc.</param>
        /// <param name="vmResourceId">ID of the VM that sent this data.</param>
        /// <param name="logger">IDiagnosticsLogger.</param>
        /// <returns>Task.</returns>
        Task ProcessAsync(CollectedData data, Guid vmResourceId, IDiagnosticsLogger logger);
    }
}
