// <copyright file="ComponentType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models
{
    /// <summary>
    /// Virtual machine components.
    /// </summary>
    public enum ComponentType
    {
        /// <summary>
        /// Network Interface.
        /// </summary>
        NetworkInterface = 0,

        /// <summary>
        /// Subnet.
        /// </summary>
        Subnet = 1,

        /// <summary>
        /// Virtual Network.
        /// </summary>
        VirtualNetwork = 2,

        /// <summary>
        /// OD Disk.
        /// </summary>
        OSDisk = 3,
    }
}