// <copyright file="IDeploymentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    public interface IDeploymentManager
    {
        Task<DeploymentStatusInput> BeginCreateAsync(VirtualMachineInstance vmInstance);

        Task<DeploymentState> CheckDeploymentStatusAsync(DeploymentStatusInput deploymentStatusInput);
    }
}