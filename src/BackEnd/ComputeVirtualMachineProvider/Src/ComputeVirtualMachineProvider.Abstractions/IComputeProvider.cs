// <copyright file="IComputeProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions
{
    /// <summary>
    ///
    /// </summary>
    public interface IComputeProvider
    {
        /// <summary>
        /// NOTE, this won't wait for Create workflow to finish
        /// this will return as soon as tracking info is ready.
        /// </summary>
        /// <param name="input">Provides input to Create Azure file share.</param>
        /// <param name="continuationToken"></param>
        /// <returns>
        ///     Result of the Create operations which includes TrackingId which
        ///     can be used to call the StatusCheckAsync to find out the status
        ///     of the create request.
        /// </returns>
        Task<VirtualMachineProviderCreateResult> CreateAsync(VirtualMachineProviderCreateInput input, string continuationToken = null);

        /// <summary>
        /// NOTE, this won't wait for Delete workflow to finish
        /// /// /// this will return as soon as tracking info is ready.
        /// </summary>
        /// <param name="input">Provides input to Delete Azure file share.</param>
        /// <param name="continuationToken"></param>
        /// <returns>
        ///     Result of the Delete operations which includes TrackingId which
        ///     can be used to call the StatusCheckAsync to find out the status
        ///     of the create request.
        /// </returns>
        Task<VirtualMachineProviderDeleteResult> DeleteAsync(VirtualMachineProviderDeleteInput input, string continuationToken = null);

        /// <summary>
        /// Prep VM to create user environment
        /// </summary>
        /// <param name="input"></param>
        /// <param name="continuationToken"></param>
        /// <returns>
        ///     Result of the Delete operations which includes TrackingId which
        ///     can be used to call the StatusCheckAsync to find out the status
        ///     of the create request.
        /// </returns>
        Task<VirtualMachineProviderAssignResult> AssignAsync(VirtualMachineProviderAssignInput input, string continuationToken = null);
    }
}
