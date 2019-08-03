// <copyright file="VirtualMachineProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    public class VirtualMachineProvider : IComputeProvider
    {
        private readonly IDeploymentManager deploymentManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualMachineProvider"/> class.
        /// </summary>
        /// <param name="deploymentManager">Create / Update / Delete VM.</param>
        public VirtualMachineProvider(IDeploymentManager deploymentManager)
        {
            Requires.NotNull(deploymentManager, nameof(deploymentManager));
            this.deploymentManager = deploymentManager;
        }

        /// <inheritdoc/>
        public async Task<VirtualMachineProviderCreateResult> CreateAsync(VirtualMachineProviderCreateInput input, string continuationToken = null)
        {
            Requires.NotNull(input, nameof(input));
            string resultContinuationToken = default;
            DeploymentState resultState;
            DeploymentStatusInput deploymentStatusInput;

            if (string.IsNullOrEmpty(continuationToken))
            {
                // create new resource
                var vmInstance = new VirtualMachineInstance()
                {
                    AzureResourceName = input.AzureResourceGroup,
                    AzureLocation = input.AzureLocation,
                    AzureResourceGroupName = input.AzureResourceGroup,
                    AzureSubscription = input.AzureSubscription,
                    AzureVmImage = input.AzureVirtualMachineImage,
                };

                deploymentStatusInput = await deploymentManager.BeginCreateAsync(vmInstance).ContinueOnAnyContext();
                resultContinuationToken = deploymentStatusInput.ToJson();
                resultState = DeploymentState.InProgress;
            }
            else
            {
                // Check status of deployment request
                deploymentStatusInput = continuationToken.ToDeploymentStatusInput();
                resultState = await deploymentManager.CheckDeploymentStatusAsync(deploymentStatusInput);
                if (resultState == DeploymentState.InProgress)
                {
                    resultContinuationToken = continuationToken;
                }
            }

            var result = new VirtualMachineProviderCreateResult()
            {
                ContinuationToken = resultContinuationToken,
                ResourceId = deploymentStatusInput.ResourceId,
                RetryAfter = TimeSpan.FromMinutes(1),
                Status = resultState.ToString(),
            };
            return result;
        }

        /// <inheritdoc/>
        public Task<VirtualMachineProviderDeleteResult> DeleteAsync(VirtualMachineProviderDeleteInput input, string continuationToken = null)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<VirtualMachineProviderAssignResult> AssignAsync(VirtualMachineProviderAssignInput input, string continuationToken = null)
        {
            throw new System.NotImplementedException();
        }
    }
}
