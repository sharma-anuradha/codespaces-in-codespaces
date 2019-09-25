// <copyright file="IDeploymentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions
{
    /// <summary>
    /// Manage Azure Virtual Machine.
    /// </summary>
    public interface IDeploymentManager
    {
        /// <summary>
        /// Used by clients to determine which manager to use for this input.
        /// </summary>
        /// <param name="computeOS">ComputeOS object.</param>
        /// <returns>true of manager handles this input.</returns>
        bool Accepts(ComputeOS computeOS);

        /// <summary>
        /// Kick off container setup on Azure Virtual Machine.
        /// </summary>
        /// <param name="input">Provides input to start cloud environment.</param>
        /// <param name="logger">Diagnostics logger object.</param>
        /// <returns>Operation state and Continuation object.</returns>
        Task<(OperationState, NextStageInput)> BeginStartComputeAsync(VirtualMachineProviderStartComputeInput input, IDiagnosticsLogger logger);

        /// <summary>
        /// Check status of container creation on Azure Virtual Machine.
        /// </summary>
        /// <param name="input">Continuation object instance.</param>
        /// <param name="logger">Diagnostics logger object.</param>
        /// <returns>Operation state and Continuation object.</returns>
        Task<(OperationState, NextStageInput)> CheckStartComputeStatusAsync(NextStageInput input, IDiagnosticsLogger logger);

        /// <summary>
        /// Kick off Azure Virtual Machine creation.
        /// </summary>
        /// <param name="input">Provides input to create azure virtual machine.</param>
        /// <param name="logger">Diagnostics logger object.</param>
        /// <returns>Operation state and Continuation object.</returns>
        Task<(OperationState, NextStageInput)> BeginCreateComputeAsync(VirtualMachineProviderCreateInput input, IDiagnosticsLogger logger);

        /// <summary>
        /// Check status of Azure Virtual Machine creation.
        /// </summary>
        /// <param name="input">Continuation object instance.</param>
        /// <param name="logger">Diagnostics logger object.</param>
        /// <returns>Operation state and Continuation object.</returns>
        Task<(OperationState, NextStageInput)> CheckCreateComputeStatusAsync(NextStageInput input, IDiagnosticsLogger logger);

        /// <summary>
        /// Kick off Azure Virtual Machine deletion.
        /// </summary>
        /// <param name="input">Provides input to delete azure virtual machine.</param>
        /// <param name="logger">Diagnostics logger object.</param>
        /// <returns>Operation state and Continuation object.</returns>
        Task<(OperationState, NextStageInput)> BeginDeleteComputeAsync(VirtualMachineProviderDeleteInput input, IDiagnosticsLogger logger);

        /// <summary>
        /// Check status of Azure Virtual Machine deletion.
        /// </summary>
        /// <param name="input">Continuation object instance.</param>
        /// <param name="logger">Diagnostics logger object.</param>
        /// <returns>Operation state and Continuation object.</returns>
        Task<(OperationState, NextStageInput)> CheckDeleteComputeStatusAsync(NextStageInput input, IDiagnosticsLogger logger);

        /// <summary>
        /// Get virtual machine input Url with Sas token with read permission.
        /// </summary>
        /// <param name="location">Virtual machine location.</param>
        /// <param name="virtualMachineName">Virtual machine name.</param>
        /// <param name="retryAttempCount">Retries attempted for this operation.</param>
        /// <param name="logger">Diagnostics logger object.</param>
        /// <returns>Virutal machine input queue sas token and url.</returns>
        Task<(OperationState, QueueConnectionInfo, int)> GetVirtualMachineInputQueueConnectionInfoAsync(
            AzureLocation location, string virtualMachineName, int retryAttempCount, IDiagnosticsLogger logger);
    }
}