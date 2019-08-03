// <copyright file="VirtualMachineInstance.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    public class VirtualMachineInstance
    {
        public string AzureLocation { get; internal set; }

        public string AzureResourceName { get; internal set; }

        public string AzureResourceGroupName { get; internal set; }

        public string AzureSubscription { get; internal set; }

        public string AzureSku => "Standard_D4s_v3";

        public string AzureVmImage { get; internal set; }

        public string GetResourceId()
        {
            return $"subscription/{AzureSubscription}/resourcegroup/{AzureResourceGroupName}/resource/{AzureResourceName}";
        }
    }
}