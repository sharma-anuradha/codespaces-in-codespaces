// <copyright file="VirtualMachineProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
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
        public async Task<VirtualMachineProviderCreateResult> CreateAsync(VirtualMachineProviderCreateInput input, IDiagnosticsLogger logger, string continuationToken = null)
        {
            Requires.NotNull(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));
            string resultContinuationToken = default;
            OperationState resultState;
            AzureResourceInfo azureResourceInfo = default;
            var duration = logger.StartDuration();

            logger = logger.WithValues(new LogValueSet
            {
                { nameof(input.AzureSubscription), input.AzureSubscription.ToString() },
                { nameof(input.AzureVmLocation), input.AzureVmLocation.ToString() },
                { nameof(input.AzureResourceGroup), input.AzureResourceGroup },
                { nameof(input.AzureSkuName), input.AzureSkuName },
                { nameof(input.AzureVirtualMachineImage), input.AzureVirtualMachineImage },
            });

            (azureResourceInfo, resultState, resultContinuationToken) = await ExecuteAsync<VirtualMachineProviderCreateInput>(
                input,
                continuationToken,
                logger,
                deploymentManager.BeginCreateComputeAsync,
                deploymentManager.CheckCreateComputeStatusAsync);

            var result = new VirtualMachineProviderCreateResult()
            {
                AzureResourceInfo = azureResourceInfo,
                Status = resultState,
                ContinuationToken = resultContinuationToken,
                RetryAfter = TimeSpan.FromSeconds(VmCreationRetryAfterSeconds),
            };

            // TODO:: Add correlation id
            logger
               .FluentAddValue(nameof(result.AzureResourceInfo), result.AzureResourceInfo.Name)
               .FluentAddValue(nameof(result.RetryAfter), result.RetryAfter.ToString())
               .FluentAddValue(nameof(result.ContinuationToken), result.ContinuationToken)
               .FluentAddValue(nameof(result.Status), result.Status.ToString())
               .AddDuration(duration)
               .LogInfo("virtual_machine_compute_provider_create_step_complete");
            return result;
        }

        /// <inheritdoc/>
        public async Task<VirtualMachineProviderDeleteResult> DeleteAsync(VirtualMachineProviderDeleteInput input, IDiagnosticsLogger logger, string continuationToken = null)
        {
            Requires.NotNull(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));
            string resultContinuationToken = default;
            OperationState resultState;
            AzureResourceInfo azureResourceInfo;
            var duration = logger.StartDuration();

            logger = logger.WithValues(new LogValueSet
            {
                { nameof(input.AzureResourceInfo.SubscriptionId), input.AzureResourceInfo.SubscriptionId.ToString() },
                { nameof(input.AzureResourceInfo.ResourceGroup), input.AzureResourceInfo.ResourceGroup },
                { nameof(input.AzureResourceInfo.Name), input.AzureResourceInfo.Name},
            });
            (azureResourceInfo, resultState, resultContinuationToken) = await ExecuteAsync<VirtualMachineProviderDeleteInput>(
                input,
                continuationToken,
                logger,
                deploymentManager.BeginDeleteComputeAsync,
                deploymentManager.CheckDeleteComputeStatusAsync);

            var result = new VirtualMachineProviderDeleteResult()
            {
                Status = resultState,
                ContinuationToken = resultContinuationToken,
                RetryAfter = TimeSpan.FromSeconds(VmDeletionRetryAfterSeconds),
            };

            // TODO:: Add correlation id
            logger
               .FluentAddValue(nameof(result.RetryAfter), result.RetryAfter.ToString())
               .FluentAddValue(nameof(result.ContinuationToken), result.ContinuationToken)
               .FluentAddValue(nameof(result.Status), result.Status.ToString())
               .AddDuration(duration)
               .LogInfo("virtual_machine_compute_provider_delete_step_complete");
            return result;
        }

        /// <inheritdoc/>
        public async Task<VirtualMachineProviderStartComputeResult> StartComputeAsync(VirtualMachineProviderStartComputeInput input, IDiagnosticsLogger logger, string continuationToken = null)
        {
            Requires.NotNull(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));
            string resultContinuationToken = default;
            OperationState resultState;
            AzureResourceInfo azureResourceInfo;
            var duration = logger.StartDuration();

            logger = logger.WithValues(new LogValueSet
            {
                { nameof(input.AzureResourceInfo.SubscriptionId), input.AzureResourceInfo.SubscriptionId.ToString() },
                { nameof(input.AzureResourceInfo.ResourceGroup), input.AzureResourceInfo.ResourceGroup },
                { nameof(input.AzureResourceInfo.Name), input.AzureResourceInfo.Name},
            });
            (azureResourceInfo, resultState, resultContinuationToken) = await ExecuteAsync<VirtualMachineProviderStartComputeInput>(
                input,
                continuationToken,
                logger,
                deploymentManager.BeginStartComputeAsync,
                deploymentManager.CheckStartComputeStatusAsync);

            var result = new VirtualMachineProviderStartComputeResult()
            {
                Status = resultState,
                ContinuationToken = resultContinuationToken,
                RetryAfter = TimeSpan.FromSeconds(VmStartEnvRetryAfterSeconds),
            };

            // TODO:: Add correlation id
            logger
               .FluentAddValue(nameof(result.RetryAfter), result.RetryAfter.ToString())
               .FluentAddValue(nameof(result.ContinuationToken), result.ContinuationToken)
               .FluentAddValue(nameof(result.Status), result.Status.ToString())
               .AddDuration(duration)
               .LogInfo("virtual_machine_compute_provider_start_compute_step_complete");
            return result;
        }

        private async Task<(AzureResourceInfo, OperationState, string)> ExecuteAsync<T>(
            T input,
            string continuationToken,
            IDiagnosticsLogger logger,
            Func<T, IDiagnosticsLogger, Task<(OperationState, NextStageInput)>> beginOperation,
            Func<NextStageInput, IDiagnosticsLogger, Task<(OperationState, NextStageInput)>> checkOperationStatus)
        {
            OperationState resultState;
            NextStageInput nextStageInput;
            string resultContinuationToken = default;

            if (string.IsNullOrEmpty(continuationToken))
            {
                (resultState, nextStageInput) = await beginOperation(input, logger);
            }
            else
            {
                // Check status of deployment request
                nextStageInput = continuationToken.ToDeploymentStatusInput();
                (resultState, nextStageInput) = await checkOperationStatus(nextStageInput, logger);
            }

            if (resultState == OperationState.InProgress)
            {
                resultContinuationToken = nextStageInput.ToJson();
            }

            return (nextStageInput.AzureResourceInfo, resultState, resultContinuationToken);
        }
    }
}