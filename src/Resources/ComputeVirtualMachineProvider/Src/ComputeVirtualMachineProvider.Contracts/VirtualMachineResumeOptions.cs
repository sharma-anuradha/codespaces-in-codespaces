// <copyright file="VirtualMachineResumeOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts
{
    /// <summary>
    /// Resume options.
    /// </summary>
    public class VirtualMachineResumeOptions : VirtualMachineProviderCreateOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the resume should be attempted via a CustomScriptExtension or a queue message.
        /// CustomScriptExtension takes long time, but it is predictable.
        /// Queue message is faster, but requires VMAgent to be running and reading off the queue.
        /// </summary>
        public bool HardBoot { get; set; }
    }
}
