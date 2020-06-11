﻿// <copyright file="IComputeProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions
{
    /// <summary>
    /// Manages virtual machine.
    /// </summary>
    public interface IComputeProvider
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
        Task<VirtualMachineProviderCreateResult> CreateAsync(VirtualMachineProviderCreateInput input, IDiagnosticsLogger logger);

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
        Task<VirtualMachineProviderDeleteResult> DeleteAsync(VirtualMachineProviderDeleteInput input, IDiagnosticsLogger logger);

        /// <summary>
        /// Prep VM to create user environment.
        /// </summary>
        /// <param name="input">Provides input to create cloud environment.</param>
        /// <param name="logger">Diagnostics logger object.</param>
        /// <returns>
        ///     Result of the Delete operations which includes TrackingId which
        ///     can be used to call the StatusCheckAsync to find out the status
        ///     of the create request.
        /// </returns>
        Task<VirtualMachineProviderStartComputeResult> StartComputeAsync(VirtualMachineProviderStartComputeInput input, IDiagnosticsLogger logger);

        /// <summary>
        /// Shuts down the vm.
        /// </summary>
        /// <param name="input">Provides input to shutdown the cloud environment.</param>
        /// <param name="diagnosticsLogger">Diagnostics logger.</param>
        /// <returns>
        ///     Result of the shutdown operation which includes the TrackingId which
        ///     can be used to call the StatusCheckAsync to find out the status
        ///     of the create request.
        /// </returns>
        Task<VirtualMachineProviderShutdownResult> ShutdownAsync(VirtualMachineProviderShutdownInput input, IDiagnosticsLogger diagnosticsLogger);

        /// <summary>
        /// Update compute tags.
        /// </summary>
        /// <param name="input">inpute.</param>
        /// <param name="logger">logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<VirtualMachineProviderUpdateTagsResult> UpdateTagsAsync(VirtualMachineProviderUpdateTagsInput input, IDiagnosticsLogger logger);
    }
}
