// <copyright file="INetworkInterfaceDeploymentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider
{
    /// <summary>
    /// Manages network interface in azure.
    /// </summary>
    public interface INetworkInterfaceDeploymentManager
    {
        /// <summary>
        /// Begin network interface deployment.
        /// </summary>
        /// <param name="input">input.</param>
        /// <param name="logger">logger.</param>
        /// <returns>result.</returns>
        Task<(OperationState OperationState, NextStageInput NextInput)> BeginCreateNetworkInterfaceAsync(
            NetworkInterfaceProviderCreateInput input,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Begin network interface deletion.
        /// </summary>
        /// <param name="input">input.</param>
        /// <param name="logger">logger.</param>
        /// <returns>result.</returns>
        Task<(OperationState OperationState, NextStageInput NextInput)> BeginDeleteNetworkInterfaceAsync(
            NetworkInterfaceProviderDeleteInput input,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Check network interface deployment status.
        /// </summary>
        /// <param name="input">input.</param>
        /// <param name="logger">logger.</param>
        /// <returns>result.</returns>
        Task<(OperationState OperationState, NextStageInput NextInput)> CheckCreateNetworkInterfaceAsync(
            NextStageInput input,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Check network interface deletion status.
        /// </summary>
        /// <param name="input">input.</param>
        /// <param name="logger">logger.</param>
        /// <returns>result.</returns>
        Task<(OperationState OperationState, NextStageInput NextInput)> CheckDeleteNetworkInterfaceAsync(
            NextStageInput input,
            IDiagnosticsLogger logger);
    }
}