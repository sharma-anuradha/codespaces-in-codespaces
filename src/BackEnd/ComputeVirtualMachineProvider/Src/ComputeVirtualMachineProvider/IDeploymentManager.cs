// <copyright file="IDeploymentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    public interface IDeploymentManager
    {
        Task<DeploymentStatusInput> BeginStartComputeAsync(VirtualMachineProviderStartComputeInput input);

        Task<DeploymentState> CheckStartComputeStatusAsync(DeploymentStatusInput input);

        Task<DeploymentStatusInput> BeginCreateComputeAsync(VirtualMachineProviderCreateInput input);

        Task<DeploymentState> CheckCreateComputeStatusAsync(DeploymentStatusInput deploymentStatusInput);
    }
}