// <copyright file="INetworkInterfaceProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeNetworkInterfaceProvider.Abstractions
{
    /// <summary>
    /// Manage Azure Virtual Machine.
    /// </summary>
    public interface INetworkInterfaceProvider
    {
        /// <summary>
        /// NOTE, this won't wait for Create workflow to finish
        /// this will return as soon as tracking info is ready.
        /// </summary>
        /// <param name="input">Provides input to Create Azure virtual machine.</param>
        /// <param name="logger">Diagnostics logger object.</param>
        /// <returns>
        ///     Result of the Create operations which includes TrackingId which
        ///     can be used to call the StatusCheckAsync to find out the status
        ///     of the create request.
        /// </returns>
        Task<NetworkInterfaceProviderCreateResult> CreateAsync(NetworkInterfaceProviderCreateInput input, IDiagnosticsLogger logger);

        /// <summary>
        /// NOTE, this won't wait for Delete workflow to finish
        /// this will return as soon as tracking info is ready.
        /// </summary>
        /// <param name="input">Provides input to Delete Azure virtual machine.</param>
        /// <param name="logger">Diagnostics logger object.</param>
        /// <returns>
        ///     Result of the Delete operations which includes TrackingId which
        ///     can be used to call the StatusCheckAsync to find out the status
        ///     of the create request.
        /// </returns>
        Task<NetworkInterfaceProviderDeleteResult> DeleteAsync(NetworkInterfaceProviderDeleteInput input, IDiagnosticsLogger logger);
    }
}