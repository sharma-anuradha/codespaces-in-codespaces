// <copyright file="IDataHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Common;

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
        /// <param name="state">Monitor State.</param>
        /// <returns>True if the handler can process the monitor state.</returns>
        bool CanProcess(AbstractMonitorState state);

        /// <summary>
        /// Specific handling for the given monitor state.
        /// </summary>
        /// <param name="state">Monitor State.</param>
        /// <param name="vmResourceId">ID of the VM that sent this state.</param>
        /// <param name="logger">IDiagnosticsLogger.</param>
        /// <returns>Task.</returns>
        Task ProcessAsync(AbstractMonitorState state, string vmResourceId, IDiagnosticsLogger logger);
    }
}
