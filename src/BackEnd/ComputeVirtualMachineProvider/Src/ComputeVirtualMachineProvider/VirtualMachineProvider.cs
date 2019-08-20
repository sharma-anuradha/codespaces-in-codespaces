// <copyright file="VirtualMachineProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    /// <summary>
    /// Creates / Manages Azure Virtual machines.
    /// </summary>
    public class VirtualMachineProvider : IComputeProvider
    {
        private const int VmCreationRetryAfterSeconds = 15;
        private const int VmDeletionRetryAfterSeconds = 5;
        private const int VmStartEnvRetryAfterSeconds = 1;
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
            ResourceId resourceId = default;

            (resourceId, resultState, resultContinuationToken) = await ExecuteAsync(
                input,
                continuationToken,
                deploymentManager.BeginCreateComputeAsync,
                deploymentManager.CheckCreateComputeStatusAsync);

            var result = new VirtualMachineProviderCreateResult()
            {
                ResourceId = resourceId,
                Status = resultState.ToString(),
                ContinuationToken = resultContinuationToken,
                RetryAfter = TimeSpan.FromSeconds(VmCreationRetryAfterSeconds),
            };
            return result;
        }

        /// <inheritdoc/>
        public async Task<VirtualMachineProviderDeleteResult> DeleteAsync(VirtualMachineProviderDeleteInput input, string continuationToken = null)
        {
            Requires.NotNull(input, nameof(input));
            string resultContinuationToken = default;
            DeploymentState resultState;

            (_, resultState, resultContinuationToken) = await ExecuteAsync(
                input,
                continuationToken,
                deploymentManager.BeginDeleteComputeAsync,
                deploymentManager.CheckDeleteComputeStatusAsync);

            var result = new VirtualMachineProviderDeleteResult()
            {
                Status = resultState.ToString(),
                ContinuationToken = resultContinuationToken,
                RetryAfter = TimeSpan.FromSeconds(VmDeletionRetryAfterSeconds),
            };

            return result;
        }

        /// <inheritdoc/>
        public async Task<VirtualMachineProviderStartComputeResult> StartComputeAsync(VirtualMachineProviderStartComputeInput input, string continuationToken = null)
        {
            Requires.NotNull(input, nameof(input));
            string resultContinuationToken = default;
            DeploymentState resultState;
            (_, resultState, resultContinuationToken) = await ExecuteAsync(
                input,
                continuationToken,
                deploymentManager.BeginStartComputeAsync,
                deploymentManager.CheckStartComputeStatusAsync);

            var result = new VirtualMachineProviderStartComputeResult()
            {
                Status = resultState.ToString(),
                ContinuationToken = resultContinuationToken,
                RetryAfter = TimeSpan.FromSeconds(VmStartEnvRetryAfterSeconds),
            };
            return result;
        }

        private async Task<(ResourceId, DeploymentState, string)> ExecuteAsync<T>(
            T input,
            string continuationToken,
            Func<T, Task<(DeploymentState, DeploymentStatusInput)>> beginOperation,
            Func<DeploymentStatusInput, Task<(DeploymentState, DeploymentStatusInput)>> checkOperationStatus)
        {
            DeploymentState resultState;
            DeploymentStatusInput deploymentStatusInput;
            string resultContinuationToken = default;

            if (string.IsNullOrEmpty(continuationToken))
            {
                (resultState, deploymentStatusInput) = await beginOperation(input);
            }
            else
            {
                // Check status of deployment request
                deploymentStatusInput = continuationToken.ToDeploymentStatusInput();
                (resultState, deploymentStatusInput) = await checkOperationStatus(deploymentStatusInput);
            }

            if (resultState == DeploymentState.InProgress)
            {
                resultContinuationToken = deploymentStatusInput.ToJson();
            }

            return (deploymentStatusInput.ResourceId, resultState, resultContinuationToken);
        }
    }
}