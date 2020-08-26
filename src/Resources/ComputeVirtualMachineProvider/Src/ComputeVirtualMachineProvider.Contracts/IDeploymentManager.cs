// <copyright file="IDeploymentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts
{
    /// <summary>
    /// Manage Azure Virtual Machine.
    /// </summary>
    public interface IDeploymentManager
    {
        /// <summary>
        /// Kick off container setup on Azure Virtual Machine.
        /// </summary>
        /// <param name="input">Provides input to start cloud environment.</param>
        /// <param name="retryAttemptCount">Provides count of retry attempt.</param>
        /// <param name="logger">Diagnostics logger object.</param>
        /// <returns>Operation state.</returns>
        Task<(OperationState OperationState, int RetryAttempt)> StartComputeAsync(
            VirtualMachineProviderStartComputeInput input,
            int retryAttemptCount,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Kick off Azure Virtual Machine creation.
        /// </summary>
        /// <param name="input">Provides input to create azure virtual machine.</param>
        /// <param name="logger">Diagnostics logger object.</param>
        /// <returns>Operation state and Continuation object.</returns>
        Task<(OperationState OperationState, NextStageInput NextInput)> BeginCreateComputeAsync(
            VirtualMachineProviderCreateInput input,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Check status of Azure Virtual Machine creation.
        /// </summary>
        /// <param name="input">Continuation object instance.</param>
        /// <param name="logger">Diagnostics logger object.</param>
        /// <returns>Operation state and Continuation object.</returns>
        Task<(OperationState OperationState, NextStageInput NextInput)> CheckCreateComputeStatusAsync(
            NextStageInput input,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Kick off Azure Virtual Machine deletion.
        /// </summary>
        /// <param name="input">Provides input to delete azure virtual machine.</param>
        /// <param name="logger">Diagnostics logger object.</param>
        /// <returns>Operation state and Continuation object.</returns>
        Task<(OperationState OperationState, NextStageInput NextInput)> BeginDeleteComputeAsync(
            VirtualMachineProviderDeleteInput input,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Check status of Azure Virtual Machine deletion.
        /// </summary>
        /// <param name="input">Continuation object instance.</param>
        /// <param name="logger">Diagnostics logger object.</param>
        /// <returns>Operation state and Continuation object.</returns>
        Task<(OperationState OperationState, NextStageInput NextInput)> CheckDeleteComputeStatusAsync(
            NextStageInput input,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Shuts down the virtual machine.
        /// </summary>
        /// <param name="input">The input info.</param>
        /// <param name="retryAttempt">Retry attempt.</param>
        /// <param name="logger">Diagnostics logger.</param>
        /// <returns>The status of the operation and retry attempt.</returns>
        Task<(OperationState OperationState, int RetryAttempt)> ShutdownComputeAsync(
            VirtualMachineProviderShutdownInput input,
            int retryAttempt,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Update compute tags.
        /// </summary>
        /// <param name="input">input.</param>
        /// <param name="logger">logger.</param>
        /// <returns>result.</returns>
        Task<OperationState> UpdateTagsAsync(
            VirtualMachineProviderUpdateTagsInput input,
            IDiagnosticsLogger logger);
        
        /// <summary>
        /// Apply nsg rules.
        /// </summary>
        /// <param name="azureResourceInfo">azureResourceInfo.</param>
        /// <param name="networkInterfaceComponent">networkInterfaceComponent.</param>
        /// <returns>result.</returns>
        Task<OperationState> ApplyNsgRulesAsync(AzureResourceInfo azureResourceInfo, ResourceComponent networkInterfaceComponent, IDiagnosticsLogger logger);
    }
}