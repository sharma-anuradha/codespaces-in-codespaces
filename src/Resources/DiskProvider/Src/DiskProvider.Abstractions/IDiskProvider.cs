// <copyright file="IDiskProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Abstractions
{
    /// <summary>
    /// Provider of Managed Disks.
    /// </summary>
    public interface IDiskProvider
    {
        /// <summary>
        /// Acquires the Operating System disk given the virtual machine information.
        /// It will also tag the OS disk with the resource tags to be tracked.
        /// Note: this is not a continuation, this call will return with the OS Disk information.
        /// </summary>
        /// <param name="input">Input parameter for get OS disk.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>Operating system disk info.</returns>
        Task<DiskProviderAcquireOSDiskResult> AcquireOSDiskAsync(DiskProviderAcquireOSDiskInput input, IDiagnosticsLogger logger);

        /// <summary>
        /// Deletes the disk.
        /// Note: This will immediately return the tracking information, with the deletion in progress on the background.
        /// </summary>
        /// <param name="input">Input parameter for deleting the disk.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>
        ///     Result of the delete operation with tracking information.
        /// </returns>
        Task<DiskProviderDeleteResult> DeleteDiskAsync(DiskProviderDeleteInput input, IDiagnosticsLogger logger);

        /// <summary>
        /// Checks if a disk is detached.
        /// Note: this is not a continuation, it will immediately return with a true or false.
        /// </summary>
        /// <param name="azureResourceInfo">Azure resource info.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>True if detached.</returns>
        Task<bool> IsDetachedAsync(AzureResourceInfo azureResourceInfo, IDiagnosticsLogger logger);
    }
}
