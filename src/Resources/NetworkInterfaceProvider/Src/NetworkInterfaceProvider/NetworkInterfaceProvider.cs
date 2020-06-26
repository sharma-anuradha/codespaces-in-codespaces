// <copyright file="NetworkInterfaceProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeNetworkInterfaceProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider
{
    /// <summary>
    /// Provides Network Interface for Virtual Machines.
    /// </summary>
    public class NetworkInterfaceProvider : INetworkInterfaceProvider
    {
        private const string LogBase = "network_interface_provider";
        private TimeSpan creationRetryInterval = TimeSpan.FromSeconds(1);
        private TimeSpan deletionRetryInterval = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkInterfaceProvider"/> class.
        /// </summary>
        /// <param name="deploymentManager">provides deployment manager.</param>
        public NetworkInterfaceProvider(INetworkInterfaceDeploymentManager deploymentManager)
        {
            DeploymentManager = Requires.NotNull(deploymentManager, nameof(deploymentManager));
        }

        private INetworkInterfaceDeploymentManager DeploymentManager { get; }

        /// <inheritdoc/>
        public Task<NetworkInterfaceProviderCreateResult> CreateAsync(NetworkInterfaceProviderCreateInput input, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                 $"{LogBase}_create",
                 async (childLogger) =>
                 {
                     string resultContinuationToken = default;
                     OperationState resultState;
                     AzureResourceInfo azureResourceInfo = default;

                     (azureResourceInfo, resultState, resultContinuationToken) = await DeploymentUtils.ExecuteOperationAsync(
                                 input,
                                 childLogger,
                                 DeploymentManager.BeginCreateNetworkInterfaceAsync,
                                 DeploymentManager.CheckCreateNetworkInterfaceAsync);

                     var result = new NetworkInterfaceProviderCreateResult()
                     {
                         AzureResourceInfo = azureResourceInfo,
                         Status = resultState,
                         RetryAfter = creationRetryInterval,
                         NextInput = input.BuildNextInput(resultContinuationToken),
                     };

                     return result;
                 },
                 (ex, childLogger) =>
                 {
                     var result = new NetworkInterfaceProviderCreateResult() { Status = OperationState.Failed, ErrorReason = ex.Message };
                     return Task.FromResult(result);
                 },
                 swallowException: true);
        }

        /// <inheritdoc/>
        public Task<NetworkInterfaceProviderDeleteResult> DeleteAsync(NetworkInterfaceProviderDeleteInput input, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                    $"{LogBase}_delete",
                    async (childLogger) =>
                    {
                        string resultContinuationToken = default;
                        OperationState resultState;
                        AzureResourceInfo azureResourceInfo = default;

                        (azureResourceInfo, resultState, resultContinuationToken) = await DeploymentUtils.ExecuteOperationAsync(
                                    input,
                                    childLogger,
                                    DeploymentManager.BeginDeleteNetworkInterfaceAsync,
                                    DeploymentManager.CheckDeleteNetworkInterfaceAsync);

                        var result = new NetworkInterfaceProviderDeleteResult()
                        {
                            Status = resultState,
                            RetryAfter = deletionRetryInterval,
                            NextInput = input.BuildNextInput(resultContinuationToken),
                        };

                        return result;
                    },
                    (ex, childLogger) =>
                    {
                        var result = new NetworkInterfaceProviderDeleteResult() { Status = OperationState.Failed, ErrorReason = ex.Message };
                        return Task.FromResult(result);
                    },
                    swallowException: true);
        }
    }
}
