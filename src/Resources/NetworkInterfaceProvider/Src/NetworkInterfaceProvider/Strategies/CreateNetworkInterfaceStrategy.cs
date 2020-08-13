// <copyright file="CreateNetworkInterfaceStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider.Strategies
{
    /// <summary>
    /// Create network interface from existing virtual network strategy.
    /// </summary>
    public class CreateNetworkInterfaceStrategy : ICreateNetworkInterfaceStrategy
    {
        private const string LogBase = "create_network_interface_strategy";

        private const string TemplateParameterKey = "Value";

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateNetworkInterfaceStrategy"/> class.
        /// </summary>
        /// <param name="clientFactory">provides azure client.</param>
        public CreateNetworkInterfaceStrategy(
            IAzureClientFPAFactory clientFactory)
        {
            ClientFactory = Requires.NotNull(clientFactory, nameof(clientFactory));
        }

        /// <inheritdoc/>
        public string NetworkInterfaceTemplateJson => GetNetworkInterfaceTemplate();

        private IClientFactory ClientFactory { get; }

        /// <inheritdoc/>
        public Task<(OperationState OperationState, NextStageInput NextInput)> BeginCreateNetworkInterface(
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
                        NetworkInterfaceTemplateJson,
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
                                Properties = new AzureResourceInfoNetworkInterfaceProperties
                                {
                                    IsVNetInjected = true,
                                },
                            },
                        });
                });
        }

        /// <inheritdoc/>
        public bool Accepts(NetworkInterfaceProviderCreateInput input)
        {
            return !string.IsNullOrEmpty(input.SubnetAzureResourceId);
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
