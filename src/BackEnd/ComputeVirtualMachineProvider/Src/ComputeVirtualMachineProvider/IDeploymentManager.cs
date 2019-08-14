// <copyright file="IDeploymentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    /// <summary>
    /// Manage Azure Virtual Machine.
    /// </summary>
    public interface IDeploymentManager
    {
        /// <summary>
        /// Kick off container setup on Azure Virtual Machine.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        Task<DeploymentStatusInput> BeginStartComputeAsync(VirtualMachineProviderStartComputeInput input);

        /// <summary>
        /// Check status of container creation on Azure Virtual Machine.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        Task<DeploymentState> CheckStartComputeStatusAsync(DeploymentStatusInput input);

        /// <summary>
        /// Kick off Azure Virtual Machine creation.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        Task<DeploymentStatusInput> BeginCreateComputeAsync(VirtualMachineProviderCreateInput input);

        /// <summary>
        /// Check status of Azure Virtual Machine creation.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        Task<DeploymentState> CheckCreateComputeStatusAsync(DeploymentStatusInput input);

        /// <summary>
        /// Kick off Azure Virtual Machine deletion.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        Task<DeploymentStatusInput> BeginDeleteComputeAsync(VirtualMachineProviderDeleteInput input);

        /// <summary>
        /// Check status of Azure Virtual Machine deletion.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        Task<DeploymentState> CheckDeleteComputeStatusAsync(DeploymentStatusInput input);
    }
}