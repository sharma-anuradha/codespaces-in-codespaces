// <copyright file="AzureDeploymentHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    public class AzureDeploymentHelper
    {
        private readonly IAzureClientFactory clientFactory;

        public AzureDeploymentHelper(IAzureClientFactory clientFactory)
        {
            this.clientFactory = clientFactory;
        }

        public async Task CreateResourceGroupAsync(Guid subscriptionId, string resourceGroupName, AzureLocation location)
        {
            IAzure azure = await clientFactory.GetAzureClientAsync(subscriptionId);
            var resourceGroup = await azure.ResourceGroups.Define(resourceGroupName)
                .WithRegion(location.ToString())
                .CreateAsync();
        }

        public async Task DeleteResourceGroupAsync(Guid subscriptionId, string resourceGroupName)
        {
            IAzure azure = await clientFactory.GetAzureClientAsync(subscriptionId);
            await azure.ResourceGroups
            .BeginDeleteByNameAsync(resourceGroupName);
        }
    }
}