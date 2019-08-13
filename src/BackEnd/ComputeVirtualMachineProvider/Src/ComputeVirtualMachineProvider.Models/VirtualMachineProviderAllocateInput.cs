// <copyright file="VirtualMachineProviderAllocateInput.cs" company="Microsoft">
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
    public class VirtualMachineProviderStartComputeInput
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="resourceId"></param>
        /// <param name="shareConnectionInfo"></param>
        /// <param name="inputParams"></param>
        public VirtualMachineProviderStartComputeInput(
            ResourceId resourceId,
            ShareConnectionInfo shareConnectionInfo,
            IDictionary<string,string> inputParams)
        {
            ResourceId = resourceId;
            FileShareConnection = shareConnectionInfo;
            VmInputParams = inputParams;
        }

        /// <summary>
        /// 
        /// </summary>
        public ResourceId ResourceId { get; }

        /// <summary>
        /// 
        /// </summary>
        public ShareConnectionInfo FileShareConnection { get; }

        /// <summary>
        /// 
        /// </summary>
        public IDictionary<string, string> VmInputParams { get; }
    }
}
