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
        public DeploymentStatusInput(string azureDeploymentName, ResourceId resourceId)
        {
            AzureDeploymentName = azureDeploymentName;
            ResourceId = resourceId;
        }

        public Guid AzureSubscription
        {
            get
            {
                return ResourceId.SubscriptionId;
            }
        }

        public string AzureResourceGroupName
        {
            get
            {
                return ResourceId.ResourceGroup;
            }
        }

        public string AzureDeploymentName { get; }

        public ResourceId ResourceId { get; }
    }
}