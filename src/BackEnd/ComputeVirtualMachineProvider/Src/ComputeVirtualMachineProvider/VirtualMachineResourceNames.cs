// <copyright file="VirtualMachineResourceNames.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    public static class VirtualMachineResourceNames
    {
        /// <summary>
        /// Gets the name of the input qeue.
        /// </summary>
        /// <param name="vmName">Virtual machine name.</param>
        /// <returns>Name of the input queue.</returns>
        public static string GetInputQueueName(string vmName) => $"{vmName.ToLowerInvariant()}-input-queue";

        /// <summary>
        /// Gets name of the Os Disk.
        /// </summary>
        /// <param name="vmName">Virtual machine name.</param>
        /// <returns>Name of the Os disk.</returns>
        public static string GetOsDiskName(string vmName) => $"{vmName}-disk";

        /// <summary>
        /// Gets name of the virtual network.
        /// </summary>
        /// <param name="vmName">Virtual machine name.</param>
        /// <returns>Name of the virtual network.</returns>
        public static string GetVirtualNetworkName(string vmName) => $"{vmName}-vnet";

        /// <summary>
        /// Gets name of the security group.
        /// </summary>
        /// <param name="vmName">Virtual machine name.</param>
        /// <returns>Name of the security group.</returns>
        public static string GetNetworkSecurityGroupName(string vmName) => $"{vmName}-nsg";

        /// <summary>
        /// Gets name of the network interface.
        /// </summary>
        /// <param name="vmName">Virtual machine name.</param>
        /// <returns>Name of the network interface.</returns>
        public static string GetNetworkInterfaceName(string vmName) => $"{vmName}-nic";
    }
}
