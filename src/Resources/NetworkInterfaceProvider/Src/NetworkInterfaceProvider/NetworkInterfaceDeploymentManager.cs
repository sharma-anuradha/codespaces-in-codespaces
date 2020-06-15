// <copyright file="NetworkInterfaceDeploymentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider
{
    /// <summary>
    /// Provides Network Interface for Virtual Machines.
    /// </summary>
    public class NetworkInterfaceDeploymentManager : INetworkInterfaceDeploymentManager
    {
        private const string TemplateParameterKey = "Value";
        private const string LogBase = "network_interface_deployment_manager";

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkInterfaceDeploymentManager"/> class.
        /// </summary>
        /// <param name="clientFactory">provides azure client.</param>
        public NetworkInterfaceDeploymentManager(
            IAzureClientFPAFactory clientFactory)
        {
            ClientFactory = Requires.NotNull(clientFactory, nameof(clientFactory));
            TemplateJson = GetNetworkInterfaceTemplate();
        }

        private IClientFactory ClientFactory { get; }

        private string TemplateJson { get; }

        /// <summary>
        /// test.
        /// </summary>
        /// <param name="input">t.</param>
        /// <param name="logger">r.</param>
        /// <returns>e.</returns>
        public Task<(OperationState OperationState, NextStageInput NextInput)> BeginCreateNetworkInterfaceAsync(
           NetworkInterfaceProviderCreateInput input,
           IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                    $"{LogBase}_begin_create",
                    async (childLogger) =>
                    {
                        Requires.NotNullOrEmpty(input.ResourceGroup, nameof(input.ResourceGroup));
                        Requires.NotNullOrEmpty(input.SubnetAzureResourceId, nameof(input.SubnetAzureResourceId));

                        var azure = await ClientFactory.GetAzureClientAsync(input.SubnetSubscription, childLogger.NewChildLogger());
                        await azure.CreateResourceGroupIfNotExistsAsync(input.ResourceGroup, input.Location.ToString());
                        var networkInterfaceName = Guid.NewGuid().ToString();
                        var parameters = new Dictionary<string, Dictionary<string, object>>()
                        {
                            { "networkInterfaceName", new Dictionary<string, object>() { { TemplateParameterKey, networkInterfaceName } } },
                            { "location", new Dictionary<string, object>() { { TemplateParameterKey, input.Location.ToString() } } },
                            { "subnetId", new Dictionary<string, object>() { { TemplateParameterKey, input.SubnetAzureResourceId } } },
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
                                    SubscriptionId = input.SubnetSubscription,
                                },
                            });
                    });
        }

        /// <inheritdoc/>
        public Task<(OperationState OperationState, NextStageInput NextInput)> CheckCreateNetworkInterfaceAsync(
            NextStageInput input,
            IDiagnosticsLogger logger)
        {
            return logger.OperationScopeWithCustomExceptionHandlingAsync(
                    $"{LogBase}_check_create_status",
                    async (childLogger) =>
                    {
                        var resource = input.AzureResourceInfo;
                        var azure = await ClientFactory.GetAzureClientAsync(resource.SubscriptionId, childLogger.NewChildLogger());
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
                    },
                    (ex, childLogger) =>
                    {
                        if (!(ex is DeploymentException))
                        {
                            if (input.RetryAttempt < 5)
                            {
                                return (true, Task.FromResult((OperationState.InProgress,
                                    new NextStageInput(input.TrackingId, input.AzureResourceInfo, input.RetryAttempt + 1))));
                            }
                        }

                        return (false, Task.FromResult(default((OperationState, NextStageInput))));
                    });
        }

        /// <inheritdoc/>
        public Task<(OperationState OperationState, NextStageInput NextInput)> BeginDeleteNetworkInterfaceAsync(
          NetworkInterfaceProviderDeleteInput input,
          IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                    $"{LogBase}_begin_delete",
                    async (childLogger) =>
                    {
                        var nicClient = await ClientFactory.GetNetworkManagementClient(input.ResourceInfo.SubscriptionId, childLogger.NewChildLogger());
                        await nicClient.NetworkInterfaces.BeginDeleteAsync(input.ResourceInfo.ResourceGroup, input.ResourceInfo.Name);
                        return (OperationState.InProgress, new NextStageInput(input.ResourceInfo.Name, input.ResourceInfo));
                    });
        }

        /// <inheritdoc/>
        public Task<(OperationState OperationState, NextStageInput NextInput)> CheckDeleteNetworkInterfaceAsync(
            NextStageInput input,
            IDiagnosticsLogger logger)
        {
            return logger.OperationScopeWithCustomExceptionHandlingAsync(
                     $"{LogBase}_check_delete_status",
                     async (childLogger) =>
                     {
                         var nicAzureClient = await ClientFactory.GetAzureClientAsync(input.AzureResourceInfo.SubscriptionId, childLogger.NewChildLogger());
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
                     },
                     (ex, childLogger) =>
                     {
                         if (input.RetryAttempt < 5)
                         {
                             return (true, Task.FromResult((OperationState.InProgress,
                                 new NextStageInput(input.TrackingId, input.AzureResourceInfo, input.RetryAttempt + 1))));
                         }

                         return (false, Task.FromResult(default((OperationState, NextStageInput))));
                     });
        }

        private string GetNetworkInterfaceTemplate()
        {
            var resourceName = "template_nic.json";
            var namespaceString = typeof(NetworkInterfaceDeploymentManager).Namespace;
            var fullyQualifiedResourceName = $"{namespaceString}.Templates.{resourceName}";
            return CommonUtils.GetEmbeddedResourceContent(fullyQualifiedResourceName);
        }
    }
}
