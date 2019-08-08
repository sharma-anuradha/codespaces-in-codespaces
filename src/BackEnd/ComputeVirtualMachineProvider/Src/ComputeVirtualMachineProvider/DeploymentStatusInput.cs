// <copyright file="DeploymentStatusInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    public class DeploymentStatusInput
    {
        public DeploymentStatusInput(Guid azureSubscription, string azureResourceGroupName, string azureDeploymentName, ResourceId resourceId)
        {
            AzureSubscription = azureSubscription;
            AzureResourceGroupName = azureResourceGroupName;
            AzureDeploymentName = azureDeploymentName;
            ResourceId = resourceId;
        }

        public Guid AzureSubscription { get; }

        public string AzureResourceGroupName { get; }

        public string AzureDeploymentName { get; }

        public ResourceId ResourceId { get; }
    }
}