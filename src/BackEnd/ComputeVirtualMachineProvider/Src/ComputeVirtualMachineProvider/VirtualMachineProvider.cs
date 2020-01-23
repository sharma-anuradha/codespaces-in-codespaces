// <copyright file="VirtualMachineProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
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
        private readonly IEnumerable<IDeploymentManager> managers = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualMachineProvider"/> class.
        /// </summary>
        /// <param name="managers">Create / Update / Delete VM.</param>
        public VirtualMachineProvider(IEnumerable<IDeploymentManager> managers)
        {
            this.managers = Requires.NotNull(managers, nameof(managers));
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
                    string resultContinuationToken = default;
                    OperationState resultState;
                    AzureResourceInfo azureResourceInfo = default;

                    var deploymentManager = SelectDeploymentManager(input.ComputeOS);

                    childLogger.FluentAddBaseValue(nameof(input.AzureSubscription), input.AzureSubscription.ToString())
                        .FluentAddBaseValue(nameof(input.AzureVmLocation), input.AzureVmLocation.ToString())
                        .FluentAddBaseValue(nameof(input.AzureResourceGroup), input.AzureResourceGroup)
                        .FluentAddBaseValue(nameof(input.AzureSkuName), input.AzureSkuName)
                        .FluentAddBaseValue(nameof(input.AzureVirtualMachineImage), input.AzureVirtualMachineImage);

                    (azureResourceInfo, resultState, resultContinuationToken) = await ExecuteAsync(
                        input,
                        childLogger,
                        deploymentManager.BeginCreateComputeAsync,
                        deploymentManager.CheckCreateComputeStatusAsync);

                    var result = new VirtualMachineProviderCreateResult()
                    {
                        AzureResourceInfo = azureResourceInfo,
                        Status = resultState,
                        RetryAfter = TimeSpan.FromSeconds(VmCreationRetryAfterSeconds),
                        NextInput = input.BuildNextInput(resultContinuationToken),
                    };

                    // TODO:: Add correlation id
                    childLogger.FluentAddValue(nameof(result.AzureResourceInfo), result.AzureResourceInfo?.Name)
                       .FluentAddValue(nameof(result.RetryAfter), result.RetryAfter.ToString())
                       .FluentAddValue(nameof(result.NextInput.ContinuationToken), result.NextInput?.ContinuationToken)
                       .FluentAddValue(nameof(result.Status), result.Status.ToString());

                    return result;
                },
                (ex, childLogger) =>
                {
                    var result = new VirtualMachineProviderCreateResult() { Status = OperationState.Failed, ErrorReason = ex.Message };
                    return Task.FromResult(result);
                },
                swallowException: true);
        }

        /// <inheritdoc/>
        public Task<VirtualMachineProviderShutdownResult> ShutdownAsync(VirtualMachineProviderShutdownInput input, IDiagnosticsLogger logger)
        {
            Requires.NotNull(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));

            return logger.OperationScopeAsync(
               "virtual_machine_compute_provider_shutdown_compute",
               async (childLogger) =>
               {
                   var deploymentManager = SelectDeploymentManager(input.ComputeOS);
                   var duration = logger.StartDuration();
                   childLogger.FluentAddBaseValue(nameof(input.AzureResourceInfo.SubscriptionId), input.AzureResourceInfo.SubscriptionId.ToString())
                        .FluentAddBaseValue(nameof(input.AzureResourceInfo.ResourceGroup), input.AzureResourceInfo.ResourceGroup)
                        .FluentAddBaseValue(nameof(input.AzureResourceInfo.Name), input.AzureResourceInfo.Name)
                        .FluentAddBaseValue(nameof(input.AzureVmLocation), input.AzureVmLocation.ToString());

                   var getRetryAttempt = int.TryParse(input.ContinuationToken, out var count);
                   var retryAttemptCount = getRetryAttempt ? count : 0;
                   var shutdownOperationResult = await deploymentManager.ShutdownComputeAsync(input, retryAttemptCount, logger);
                   var result = new VirtualMachineProviderShutdownResult
                   {
                       Status = shutdownOperationResult.Item1,
                       NextInput = (shutdownOperationResult.Item1 == OperationState.Succeeded) ? default : input.BuildNextInput(shutdownOperationResult.Item2.ToString()),
                   };
                   childLogger.FluentAddValue(nameof(result.Status), result.Status.ToString())
                        .FluentAddValue(nameof(result.NextInput), result.NextInput?.ToString());
                   return result;
               },
               (ex, childLogger) =>
               {
                   var result = new VirtualMachineProviderShutdownResult() { Status = OperationState.Failed, ErrorReason = ex.Message };
                   return Task.FromResult(result);
               },
               swallowException: true);
        }

        /// <inheritdoc/>
        public Task<VirtualMachineProviderDeleteResult> DeleteAsync(VirtualMachineProviderDeleteInput input, IDiagnosticsLogger logger)
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
                    var deploymentManager = SelectDeploymentManager(input.ComputeOS);
                    var duration = childLogger.StartDuration();

                    childLogger.FluentAddBaseValue(nameof(input.AzureResourceInfo.SubscriptionId), input.AzureResourceInfo.SubscriptionId.ToString())
                        .FluentAddBaseValue(nameof(input.AzureResourceInfo.ResourceGroup), input.AzureResourceInfo.ResourceGroup)
                        .FluentAddBaseValue(nameof(input.AzureResourceInfo.Name), input.AzureResourceInfo.Name)
                        .FluentAddBaseValue(nameof(input.AzureVmLocation), input.AzureVmLocation.ToString());

                    (azureResourceInfo, resultState, resultContinuationToken) = await ExecuteAsync<VirtualMachineProviderDeleteInput>(
                        input,
                        childLogger,
                        deploymentManager.BeginDeleteComputeAsync,
                        deploymentManager.CheckDeleteComputeStatusAsync);

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

                    // Use linux provider as this path will be same for windows and linux VMs.
                    // TODO:: move common pieces in abstract base class.
                    var deploymentManager = SelectDeploymentManager(ComputeOS.Linux);

                    var getRetryAttempt = int.TryParse(input.ContinuationToken, out var count);
                    var retryAttemptCount = getRetryAttempt ? count : 0;
                    var startComputeResult = await deploymentManager.StartComputeAsync(input, retryAttemptCount, childLogger.NewChildLogger());
                    var result = new VirtualMachineProviderStartComputeResult()
                    {
                        Status = startComputeResult.Item1,
                        NextInput = (startComputeResult.Item1 == OperationState.Succeeded) ? default : input.BuildNextInput(startComputeResult.Item2.ToString()),
                    };

                    // TODO:: Add correlation id
                    childLogger.FluentAddValue(nameof(result.Status), result.Status.ToString())
                       .FluentAddValue(nameof(result.NextInput), result.NextInput?.ToString());

                    return result;
                },
                (ex, childLogger) =>
                {
                    var result = new VirtualMachineProviderStartComputeResult() { Status = OperationState.Failed, ErrorReason = ex.Message };
                    return Task.FromResult(result);
                },
                swallowException: true);
        }

        private async Task<(AzureResourceInfo, OperationState, string)> ExecuteAsync<T>(
            T input,
            IDiagnosticsLogger logger,
            Func<T, IDiagnosticsLogger, Task<(OperationState, NextStageInput)>> beginOperation,
            Func<NextStageInput, IDiagnosticsLogger, Task<(OperationState, NextStageInput)>> checkOperationStatus)
            where T : ContinuationInput
        {
            OperationState resultState;
            NextStageInput nextStageInput;
            string resultContinuationToken = default;
            var continuationToken = input.ContinuationToken;

            if (string.IsNullOrEmpty(continuationToken))
            {
                (resultState, nextStageInput) = await beginOperation(input, logger);
            }
            else
            {
                // Check status of deployment request
                nextStageInput = continuationToken.ToNextStageInput();
                (resultState, nextStageInput) = await checkOperationStatus(nextStageInput, logger);
            }

            if (resultState == OperationState.InProgress)
            {
                resultContinuationToken = nextStageInput.ToJson();
            }

            return (nextStageInput?.AzureResourceInfo, resultState, resultContinuationToken);
        }

        private IDeploymentManager SelectDeploymentManager(ComputeOS computeOS)
        {
            var acceptsDeploymentManager = managers.Where(x => x.Accepts(computeOS));
            if (acceptsDeploymentManager == null || acceptsDeploymentManager.Count() != 1)
            {
                throw new NotSupportedException($"One and only one deployment manager is allowed to process request.");
            }

            return acceptsDeploymentManager.First();
        }
    }
}