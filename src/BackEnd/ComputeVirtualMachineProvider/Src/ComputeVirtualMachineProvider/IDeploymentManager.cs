// <copyright file="IDeploymentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    public interface IDeploymentManager
    {
        Task<DeploymentStatusInput> BeginCreateAsync(VirtualMachineInstance vmInstance);

        Task<DeploymentState> CheckDeploymentStatusAsync(DeploymentStatusInput deploymentStatusInput);

        Task<DeploymentState> DeleteVMAsync(ResourceId resourceId);
    }
}