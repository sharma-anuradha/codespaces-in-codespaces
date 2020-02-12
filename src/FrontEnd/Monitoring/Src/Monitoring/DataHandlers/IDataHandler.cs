// <copyright file="IDataHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;

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
        /// <param name="handlerContext">The new state information from the handlers.</param>
        /// <param name="vmResourceId">ID of the VM that sent this data.</param>
        /// <param name="logger">IDiagnosticsLogger.</param>
        /// <returns>Returns the state information from the handler.</returns>
        Task<CollectedDataHandlerContext> ProcessAsync(CollectedData data, CollectedDataHandlerContext handlerContext, Guid vmResourceId, IDiagnosticsLogger logger);
    }
}
