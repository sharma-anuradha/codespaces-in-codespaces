// <copyright file="CreateNetworkInterfaceAndVNetStrategy.cs" company="Microsoft">
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
    /// Create network interface and virtual network strategy.
    /// </summary>
    public class CreateNetworkInterfaceAndVNetStrategy : ICreateNetworkInterfaceStrategy
    {
        private const string LogBase = "create_network_interface_and_vnet_strategy";

        private const string TemplateParameterKey = "Value";

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateNetworkInterfaceAndVNetStrategy"/> class.
        /// </summary>
        /// <param name="clientFactory">provides azure client.</param>
        public CreateNetworkInterfaceAndVNetStrategy(
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
                    Requires.Argument(input.SubnetSubscription != default, nameof(input.SubnetSubscription), $"{nameof(input.SubnetSubscription)} must not be default");
                    Requires.NotNullOrEmpty(input.ResourceGroup, nameof(input.ResourceGroup));

                    var azure = await ClientFactory.GetAzureClientAsync(input.SubnetSubscription, childLogger.NewChildLogger());
                    await azure.CreateResourceGroupIfNotExistsAsync(input.ResourceGroup, input.Location.ToString());
                    var rootId = Guid.NewGuid().ToString();

                    var nsgName = $"{rootId}-nsg";
                    var vnetName = $"{rootId}-vnet";
                    var networkInterfaceName = $"{rootId}-ni";

                    var parameters = new Dictionary<string, Dictionary<string, object>>()
                    {
                        { "nsgName", new Dictionary<string, object>() { { TemplateParameterKey, nsgName } } },
                        { "vnetName", new Dictionary<string, object>() { { TemplateParameterKey, vnetName } } },
                        { "niName", new Dictionary<string, object>() { { TemplateParameterKey, networkInterfaceName } } },
                        { "location", new Dictionary<string, object>() { { TemplateParameterKey, input.Location.ToString() } } },
                        { "resourceTags", new Dictionary<string, object>() { { TemplateParameterKey, input.ResourceTags } } },
                    };
                    var deploymentName = $"Create-NicAndVnet-{rootId}";

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
                                    Nsg = nsgName,
                                    VNet = vnetName,
                                    IsVNetInjected = false,
                                },
                            },
                        });
                });
        }

        /// <inheritdoc/>
        public bool Accepts(NetworkInterfaceProviderCreateInput input)
        {
            return string.IsNullOrEmpty(input.SubnetAzureResourceId);
        }

        private string GetNetworkInterfaceTemplate()
        {
            var resourceName = "template_nic_and_vnet.json";
            var namespaceString = typeof(NetworkInterfaceDeploymentManager).Namespace;
            var fullyQualifiedResourceName = $"{namespaceString}.Templates.{resourceName}";
            return CommonUtils.GetEmbeddedResourceContent(fullyQualifiedResourceName);
        }
    }
}
