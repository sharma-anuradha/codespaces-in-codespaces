// <copyright file="VirtualMachineNetworkInterfaceProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeNetworkInterfaceProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider
{
    /// <summary>
    /// Provides Network Interface for Virtual Machines.
    /// </summary>
    public class VirtualMachineNetworkInterfaceProvider : INetworkInterfaceProvider
    {
        private const string TemplateParameterKey = "Value";
        private const string LogBase = "network_interface_provider";
        private TimeSpan creationRetryInterval = TimeSpan.FromSeconds(1);
        private TimeSpan deletionRetryInterval = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualMachineNetworkInterfaceProvider"/> class.
        /// </summary>
        /// <param name="clientFactory">provides azure client.</param>
        public VirtualMachineNetworkInterfaceProvider(
            IAzureClientFactory clientFactory)
        {
            ClientFactory = Requires.NotNull(clientFactory, nameof(clientFactory));
            TemplateJson = GetNetworkInterfaceTemplate();
        }

        private IAzureClientFactory ClientFactory { get; }

        private string TemplateJson { get; }

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
                                 BeginCreateNetworkInterfaceAsync,
                                 CheckCreateNetworkInterfaceAsync);

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
                                    BeginDeleteNetworkInterfaceAsync,
                                    CheckDeleteNetworkInterfaceAsync);

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

        private async Task<(OperationState OperationState, NextStageInput NextInput)> BeginCreateNetworkInterfaceAsync(
           NetworkInterfaceProviderCreateInput input,
           IDiagnosticsLogger logger)
        {
            try
            {
                Requires.NotNullOrEmpty(input.ResourceGroup, nameof(input.ResourceGroup));
                Requires.NotNullOrEmpty(input.VnetName, nameof(input.VnetName));
                Requires.NotNullOrEmpty(input.SubnetName, nameof(input.SubnetName));
                var azure = await ClientFactory.GetAzureClientAsync(input.Subscription);
                await azure.CreateResourceGroupIfNotExistsAsync(input.ResourceGroup, input.Location.ToString());
                var networkInterfaceName = Guid.NewGuid().ToString();
                var parameters = new Dictionary<string, Dictionary<string, object>>()
                {
                    { "networkInterfaceName", new Dictionary<string, object>() { { TemplateParameterKey, networkInterfaceName } } },
                    { "location", new Dictionary<string, object>() { { TemplateParameterKey, input.Location.ToString() } } },
                    { "vnetSubscription", new Dictionary<string, object>() { { TemplateParameterKey, input.Subscription.ToString() } } },
                    { "vnetResourceGroup", new Dictionary<string, object>() { { TemplateParameterKey, input.ResourceGroup } } },
                    { "vnetName", new Dictionary<string, object>() { { TemplateParameterKey, input.VnetName } } },
                    { "subnetName", new Dictionary<string, object>() { { TemplateParameterKey, input.SubnetName } } },
                    { "resourceTags", new Dictionary<string, object>() { { TemplateParameterKey, input.ResourceTags } } },
                };
                var deploymentName = $"Create-NetworkInterface-{networkInterfaceName}";
                var result = await DeploymentUtils.BeginCreateArmResource(
                    input.ResourceGroup,
                    azure,
                    TemplateJson,
                    parameters,
                    deploymentName);

                return (OperationState.InProgress,
                    new NextStageInput()
                    {
                        TrackingId = result.Name,
                        AzureResourceInfo = new AzureResourceInfo()
                        {
                            Name = networkInterfaceName,
                            ResourceGroup = input.ResourceGroup,
                            SubscriptionId = input.Subscription,
                        },
                    });
            }
            catch (Exception ex)
            {
                logger.LogException($"{LogBase}_begin_create_error", ex);
                throw;
            }
        }

        private async Task<(OperationState OperationState, NextStageInput NextInput)> CheckCreateNetworkInterfaceAsync(
            NextStageInput input,
            IDiagnosticsLogger logger)
        {
            try
            {
                var resource = input.AzureResourceInfo;
                var azure = await ClientFactory.GetAzureClientAsync(resource.SubscriptionId);
                var operationState = await DeploymentUtils.CheckArmResourceDeploymentState(
                    azure,
                    input.TrackingId,
                    resource.ResourceGroup);

                return (operationState,
                     new NextStageInput()
                     {
                         TrackingId = input.TrackingId,
                         AzureResourceInfo = input.AzureResourceInfo,
                     });
            }
            catch (DeploymentException deploymentException)
            {
                logger.LogException($"{LogBase}_check_create_status_error", deploymentException);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogException($"{LogBase}_check_create_status_error", ex);
                if (input.RetryAttempt < 5)
                {
                    return (OperationState.InProgress, new NextStageInput(input.TrackingId, input.AzureResourceInfo, input.RetryAttempt + 1));
                }

                throw;
            }
        }

        private async Task<(OperationState OperationState, NextStageInput NextInput)> BeginDeleteNetworkInterfaceAsync(
          NetworkInterfaceProviderDeleteInput input,
          IDiagnosticsLogger logger)
        {
            try
            {
                var nicClient = await ClientFactory.GetNetworkManagementClient(input.ResourceInfo.SubscriptionId);
                await nicClient.NetworkInterfaces.BeginDeleteAsync(input.ResourceInfo.ResourceGroup, input.ResourceInfo.Name);
                return (OperationState.InProgress, new NextStageInput(input.ResourceInfo.Name, input.ResourceInfo));
            }
            catch (Exception ex)
            {
                logger.LogException($"{LogBase}_begin_delete_error", ex);
                throw;
            }
        }

        private async Task<(OperationState OperationState, NextStageInput NextInput)> CheckDeleteNetworkInterfaceAsync(
            NextStageInput input,
            IDiagnosticsLogger logger)
        {
            try
            {
                var nicAzureClient = await ClientFactory.GetAzureClientAsync(input.AzureResourceInfo.SubscriptionId);
                var resource = await nicAzureClient.NetworkInterfaces.GetByResourceGroupAsync(
                    input.AzureResourceInfo.ResourceGroup,
                    input.AzureResourceInfo.Name);
                var operationState = OperationState.InProgress;
                if (resource == default)
                {
                    operationState = OperationState.Succeeded;
                }

                return (operationState,
                     new NextStageInput()
                     {
                         TrackingId = input.TrackingId,
                         AzureResourceInfo = input.AzureResourceInfo,
                     });
            }
            catch (Exception ex)
            {
                logger.LogException($"{LogBase}_check_delete_status_error", ex);
                if (input.RetryAttempt < 5)
                {
                    return (OperationState.InProgress, new NextStageInput(input.TrackingId, input.AzureResourceInfo, input.RetryAttempt + 1));
                }

                throw;
            }
        }

        private string GetNetworkInterfaceTemplate()
        {
            var resourceName = "template_nic.json";
            var namespaceString = typeof(VirtualMachineNetworkInterfaceProvider).Namespace;
            return $"{namespaceString}.Templates.{resourceName}";
        }
    }
}
