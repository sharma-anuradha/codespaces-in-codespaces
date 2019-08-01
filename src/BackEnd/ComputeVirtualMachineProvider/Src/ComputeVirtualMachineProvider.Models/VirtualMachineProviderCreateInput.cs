// <copyright file="VirtualMachineProviderCreateInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models
{
    public class VirtualMachineProviderCreateInput
    {
        /// <summary>
        /// 
        /// </summary>
        public string AzureSubscription { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string AzureLocation { get; set; } // 'westus2'

        /// <summary>
        /// 
        /// </summary>
        public string AzureResourceGroup { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string SkuName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string AzureVirtualMachineImage { get; set; } // 'Canonical:UbuntuServer:18.04-LTS:latest'
    }
}