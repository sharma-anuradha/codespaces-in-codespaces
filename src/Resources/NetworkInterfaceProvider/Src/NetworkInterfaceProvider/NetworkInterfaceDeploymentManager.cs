// <copyright file="NetworkInterfaceDeploymentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider.Strategies;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider
{
    /// <summary>
    /// Provides Network Interface for Virtual Machines.
    /// </summary>
    public class NetworkInterfaceDeploymentManager : INetworkInterfaceDeploymentManager
    {
        private const string LogBase = "network_interface_deployment_manager";

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkInterfaceDeploymentManager"/> class.
        /// </summary>
        /// <param name="clientFactory">provides azure client.</param>
        /// <param name="createNetworkInterfaceStrategies">network interface creation strategies.</param>
        public NetworkInterfaceDeploymentManager(
            IAzureClientFPAFactory clientFactory,
            IEnumerable<ICreateNetworkInterfaceStrategy> createNetworkInterfaceStrategies)
        {
            ClientFactory = Requires.NotNull(clientFactory, nameof(clientFactory));
            Requires.NotNullOrEmpty(createNetworkInterfaceStrategies, nameof(createNetworkInterfaceStrategies));
            CreateNetworkInterfaceStrategies = createNetworkInterfaceStrategies;
        }

        private IClientFactory ClientFactory { get; }

        private IEnumerable<ICreateNetworkInterfaceStrategy> CreateNetworkInterfaceStrategies { get; }

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
            try
            {
                var createStrategy = CreateNetworkInterfaceStrategies.Where(s => s.Accepts(input)).Single();
                return createStrategy.BeginCreateNetworkInterface(input, logger);
            }
            catch (Exception ex)
            {
                logger.LogException($"{LogBase}_begin_create_error", ex);
                throw;
            }
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
    }
}
