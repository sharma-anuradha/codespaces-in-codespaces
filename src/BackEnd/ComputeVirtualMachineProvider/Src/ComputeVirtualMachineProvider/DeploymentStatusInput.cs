// <copyright file="DeploymentStatusInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    public class DeploymentStatusInput
    {
        public DeploymentStatusInput(string azureSubscription, string azureResourceGroupName, string azureDeploymentName, string resourceId)
        {
            AzureSubscription = azureSubscription;
            AzureResourceGroupName = azureResourceGroupName;
            AzureDeploymentName = azureDeploymentName;
            ResourceId = resourceId;
        }

        public string AzureSubscription { get; }

        public string AzureResourceGroupName { get; }

        public string AzureDeploymentName { get; }

        public string ResourceId { get; }
    }
}