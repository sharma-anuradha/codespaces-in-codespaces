﻿using StorageFileShareProvider.Models;
using System.Threading.Tasks;

namespace StorageFileShareProvider.Abstractions
{
    public interface IStorageProvider
    {
        /// <summary>
        /// </summary>
        /// <param name="input">Provides input to Create Azure file share.</param>
        /// <param name="continuationToken"></param>
        /// <returns>
        ///     Result of the Create operations which includes TrackingId which
        ///     can be used to call the StatusCheckAsync to find out the status
        ///     of the create request.
        /// </returns>
        Task<FileShareProviderCreateResult> CreateAsync(FileShareProviderCreateInput input, string continuationToken = null);

        /// <summary>
        /// </summary>
        /// <param name="input">Provides input to Delete Azure file share.</param>
        /// <param name="continuationToken"></param>
        /// <returns>
        ///     Result of the Delete operations which includes TrackingId which
        ///     can be used to call the StatusCheckAsync to find out the status
        ///     of the create request.
        /// </returns>
        Task<FileShareProviderDeleteResult> DeleteAsync(FileShareProviderDeleteInput input, string continuationToken = null);

        //Task<FileShareProviderAssignResult> AssignAsync(FileShareProviderAssignInput input, string continuationToken = null);
    }
}