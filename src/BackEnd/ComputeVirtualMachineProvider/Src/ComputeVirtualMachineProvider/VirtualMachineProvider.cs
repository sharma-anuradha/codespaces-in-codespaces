// <copyright file="VirtualMachineProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
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
        private const int VmCreationRetryAfterSeconds = 60;
        private const int VmDeletionRetryAfterSeconds = 60;
        private readonly IDeploymentManager manager = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualMachineProvider"/> class.
        /// </summary>
        /// <param name="manager">Create / Update / Delete VM.</param>
        public VirtualMachineProvider(IDeploymentManager manager)
        {
            this.manager = Requires.NotNull(manager, nameof(manager));
        }

        /// <inheritdoc/>
        public Task<VirtualMachineProviderCreateResult> CreateAsync(
            VirtualMachineProviderCreateInput input, IDiagnosticsLogger logger)
        {
            Requires.NotNull(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));

            return logger.OperationScopeAsync(
                "virtual_machine_compute_provider_create",
                async (childLogger) =>
                {
                    var deploymentManager = manager;

                    childLogger.FluentAddBaseValue(nameof(input.AzureSubscription), input.AzureSubscription.ToString())
                        .FluentAddBaseValue(nameof(input.AzureVmLocation), input.AzureVmLocation.ToString())
                        .FluentAddBaseValue(nameof(input.AzureResourceGroup), input.AzureResourceGroup)
                        .FluentAddBaseValue(nameof(input.AzureSkuName), input.AzureSkuName)
                        .FluentAddBaseValue(nameof(input.AzureVirtualMachineImage), input.AzureVirtualMachineImage);

                    string resultContinuationToken = default;
                    OperationState resultState;
                    AzureResourceInfo azureResourceInfo = default;

                    (azureResourceInfo, resultState, resultContinuationToken) = await DeploymentUtils.ExecuteOperationAsync(
                                input,
                                childLogger,
                                manager.BeginCreateComputeAsync,
                                manager.CheckCreateComputeStatusAsync);
                    var vmComponents = new ResourceComponentDetail()
                    {
                        Items = input.CustomComponents.ToComponentDictionary(),
                    };
                    var result = new VirtualMachineProviderCreateResult()
                    {
                        AzureResourceInfo = azureResourceInfo,
                        Components = vmComponents,
                        Status = resultState,
                        RetryAfter = TimeSpan.FromSeconds(VmCreationRetryAfterSeconds),
                        NextInput = input.BuildNextInput(resultContinuationToken),
                    };

                    childLogger.FluentAddValue(nameof(result.AzureResourceInfo), result.AzureResourceInfo?.Name)
                       .FluentAddValue(nameof(result.RetryAfter), result.RetryAfter.ToString())
                       .FluentAddValue(nameof(result.NextInput.ContinuationToken), result.NextInput?.ContinuationToken)
                       .FluentAddValue(nameof(result.Status), result.Status.ToString());

                    return result;
                },
                (ex, childLogger) =>
                {
                    var result = new VirtualMachineProviderCreateResult() { Status = OperationState.Failed, ErrorReason = ex.Message };
                    childLogger.FluentAddValue(nameof(result.Status), result.Status.ToString())
                                   .FluentAddValue(nameof(result.ErrorReason), result.ErrorReason);
                    return Task.FromResult(result);
                },
                swallowException: true);
        }

        /// <inheritdoc/>
        public Task<VirtualMachineProviderShutdownResult> ShutdownAsync(
            VirtualMachineProviderShutdownInput input, IDiagnosticsLogger logger)
        {
            Requires.NotNull(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));

            return logger.OperationScopeAsync(
               "virtual_machine_compute_provider_shutdown_compute",
               async (childLogger) =>
               {
                   childLogger.FluentAddBaseValue(nameof(input.AzureResourceInfo.SubscriptionId), input.AzureResourceInfo.SubscriptionId.ToString())
                        .FluentAddBaseValue(nameof(input.AzureResourceInfo.ResourceGroup), input.AzureResourceInfo.ResourceGroup)
                        .FluentAddBaseValue(nameof(input.AzureResourceInfo.Name), input.AzureResourceInfo.Name)
                        .FluentAddBaseValue(nameof(input.AzureVmLocation), input.AzureVmLocation.ToString());

                   var getRetryAttempt = int.TryParse(input.ContinuationToken, out var count);
                   var retryAttemptCount = getRetryAttempt ? count : 0;
                   var shutdownOperationResult = await manager.ShutdownComputeAsync(input, retryAttemptCount, logger);
                   var result = new VirtualMachineProviderShutdownResult
                   {
                       Status = shutdownOperationResult.OperationState,
                       NextInput = (shutdownOperationResult.OperationState == OperationState.Succeeded) ? default : input.BuildNextInput(shutdownOperationResult.RetryAttempt.ToString()),
                   };
                   childLogger.FluentAddValue(nameof(result.Status), result.Status.ToString())
                        .FluentAddValue(nameof(result.NextInput), result.NextInput?.ToString());
                   return result;
               },
               (ex, childLogger) =>
               {
                   var result = new VirtualMachineProviderShutdownResult() { Status = OperationState.Failed, ErrorReason = ex.Message };
                   childLogger.FluentAddValue(nameof(result.Status), result.Status.ToString())
                       .FluentAddValue(nameof(result.ErrorReason), result.ErrorReason);
                   return Task.FromResult(result);
               },
               swallowException: true);
        }

        /// <inheritdoc/>
        public Task<VirtualMachineProviderDeleteResult> DeleteAsync(
            VirtualMachineProviderDeleteInput input, IDiagnosticsLogger logger)
        {
            Requires.NotNull(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));

            return logger.OperationScopeAsync(
                "virtual_machine_compute_provider_delete",
                async (childLogger) =>
                {
                    string resultContinuationToken = default;
                    OperationState resultState;
                    AzureResourceInfo azureResourceInfo;

                    childLogger.FluentAddBaseValue(nameof(input.AzureResourceInfo.SubscriptionId), input.AzureResourceInfo.SubscriptionId.ToString())
                        .FluentAddBaseValue(nameof(input.AzureResourceInfo.ResourceGroup), input.AzureResourceInfo.ResourceGroup)
                        .FluentAddBaseValue(nameof(input.AzureResourceInfo.Name), input.AzureResourceInfo.Name)
                        .FluentAddBaseValue(nameof(input.AzureVmLocation), input.AzureVmLocation.ToString());

                    (azureResourceInfo, resultState, resultContinuationToken) = await DeploymentUtils.ExecuteOperationAsync(
                        input,
                        childLogger,
                        manager.BeginDeleteComputeAsync,
                        manager.CheckDeleteComputeStatusAsync);

                    var result = new VirtualMachineProviderDeleteResult()
                    {
                        Status = resultState,
                        RetryAfter = TimeSpan.FromSeconds(VmDeletionRetryAfterSeconds),
                        NextInput = input.BuildNextInput(resultContinuationToken),
                    };

                    // TODO:: Add correlation id
                    childLogger
               .FluentAddValue(nameof(result.RetryAfter), result.RetryAfter.ToString())
               .FluentAddValue(nameof(result.NextInput.ContinuationToken), result.NextInput?.ContinuationToken)
               .FluentAddValue(nameof(result.Status), result.Status.ToString());

                    return result;
                },
                (ex, childLogger) =>
                {
                    var result = new VirtualMachineProviderDeleteResult() { Status = OperationState.Failed, ErrorReason = ex.Message };
                    childLogger.FluentAddValue(nameof(result.Status), result.Status.ToString())
                       .FluentAddValue(nameof(result.ErrorReason), result.ErrorReason);
                    return Task.FromResult(result);
                },
                swallowException: true);
        }

        /// <inheritdoc/>
        public Task<VirtualMachineProviderStartComputeResult> StartComputeAsync(
            VirtualMachineProviderStartComputeInput input, IDiagnosticsLogger logger)
        {
            Requires.NotNull(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));

            return logger.OperationScopeAsync(
                "virtual_machine_compute_provider_start_compute",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue(nameof(input.AzureResourceInfo.SubscriptionId), input.AzureResourceInfo.SubscriptionId.ToString())
                        .FluentAddBaseValue(nameof(input.AzureResourceInfo.ResourceGroup), input.AzureResourceInfo.ResourceGroup)
                        .FluentAddBaseValue(nameof(input.AzureResourceInfo.Name), input.AzureResourceInfo.Name);

                    var getRetryAttempt = int.TryParse(input.ContinuationToken, out var count);
                    var retryAttemptCount = getRetryAttempt ? count : 0;
                    var startComputeResult = await manager.StartComputeAsync(input, retryAttemptCount, childLogger.NewChildLogger());
                    var result = new VirtualMachineProviderStartComputeResult()
                    {
                        Status = startComputeResult.OperationState,
                        NextInput = (startComputeResult.OperationState == OperationState.Succeeded) ? default : input.BuildNextInput(startComputeResult.RetryAttempt.ToString()),
                    };

                    // TODO:: Add correlation id
                    childLogger.FluentAddValue(nameof(result.Status), result.Status.ToString())
               .FluentAddValue(nameof(result.NextInput), result.NextInput?.ToString());

                    return result;
                },
                (ex, childLogger) =>
                {
                    var result = new VirtualMachineProviderStartComputeResult() { Status = OperationState.Failed, ErrorReason = ex.Message };
                    childLogger.FluentAddValue(nameof(result.Status), result.Status.ToString())
                       .FluentAddValue(nameof(result.ErrorReason), result.ErrorReason);
                    return Task.FromResult(result);
                },
                swallowException: true);
        }
    }
}