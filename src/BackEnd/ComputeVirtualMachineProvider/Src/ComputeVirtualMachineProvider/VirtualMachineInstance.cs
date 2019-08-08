// <copyright file="VirtualMachineInstance.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Azure.Management.BatchAI.Fluent.Models;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using ResourceId = Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models.ResourceId;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    public class VirtualMachineInstance
    {
        public AzureLocation AzureLocation { get; internal set; }

        public Guid AzureInstanceId { get; internal set; }

        public string AzureResourceGroupName { get; internal set; }

        public Guid AzureSubscription { get; internal set; }

        public string AzureSku => "Standard_D4s_v3";

        public string AzureVmImage { get; internal set; }

        public ResourceId GetResourceId()
        {
            return new ResourceId(ResourceType.ComputeVM, AzureInstanceId, AzureSubscription, AzureResourceGroupName, AzureLocation);
        }
    }
}