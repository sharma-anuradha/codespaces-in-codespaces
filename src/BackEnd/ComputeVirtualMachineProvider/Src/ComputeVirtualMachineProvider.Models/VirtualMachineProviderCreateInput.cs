// <copyright file="VirtualMachineProviderCreateInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

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


/*

SKU CATALOG DEFINITION EXAMPLE

'CE_Small_Linux_1' ->
    Storage - Premium, 8GB
    Compute Family - F Series
    Compute Sku - FS4_v3
    CEUnits - 10
    OS - Linux
'CE_Medium_Linux_1' ->
    Storage - Premium, 32GB
    Compute Family - F Series
    Compute Sku - FS16_v3
    CEUnits - 20
    OS - Linux
'CE_Large_Linux_1' ->
    Storage - Premium, 64GB
    Compute Family - F Series
    Compute Sku - FS32_v3
    CEUnits - 30
    OS - Linux


*/
