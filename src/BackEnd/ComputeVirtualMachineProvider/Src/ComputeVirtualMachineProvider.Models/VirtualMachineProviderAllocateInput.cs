// <copyright file="VirtualMachineProviderAssignInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models
{
    /// <summary>
    ///
    /// </summary>
    public class VirtualMachineProviderAllocateInput
    {
        public VirtualMachineProviderAllocateInput(ResourceId resourceId, ShareConnectionInfo shareConnectionInfo, Dictionary<string,string> inputParams)
        {
            this.ResourceId = resourceId;
            this.FileShareConnection = shareConnectionInfo;
            this.VmInputParams = inputParams;
        }
        public ResourceId ResourceId { get; }
        public ShareConnectionInfo FileShareConnection { get; }
        public Dictionary<string, string> VmInputParams { get; }
    }
}
