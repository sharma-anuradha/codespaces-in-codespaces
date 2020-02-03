// <copyright file="MockStorageProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Mocks
{
    /// <summary>
    /// Mock storage provider.
    /// </summary>
    public class MockStorageProvider : BaseMockResourceProvider, IStorageProvider
    {
        /// <inheritdoc/>
        public async Task<FileShareProviderAssignResult> AssignAsync(FileShareProviderAssignInput input, IDiagnosticsLogger logger)
        {
            var result = await RunAsync<FileShareProviderAssignInput, FileShareProviderAssignResult>(input, logger);
            if (result.Status == OperationState.Succeeded)
            {
                result.StorageAccountKey = "MyAccountKey";
                result.StorageAccountName = "MyAccountName";
                result.StorageFileName = "MyFileName";
                result.StorageShareName = "MyShareName";
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<FileShareProviderCreateResult> CreateAsync(FileShareProviderCreateInput input, IDiagnosticsLogger logger)
        {
            return await RunAsync<FileShareProviderCreateInput, FileShareProviderCreateResult>(input, logger);
        }

        /// <inheritdoc/>
        public async Task<FileShareProviderDeleteResult> DeleteAsync(FileShareProviderDeleteInput input, IDiagnosticsLogger logger)
        {
            return await RunAsync<FileShareProviderDeleteInput, FileShareProviderDeleteResult>(input, logger);
        }

        /// <inheritdoc/>
        public Task<FileShareProviderArchiveResult> ArchiveAsync(FileShareProviderArchiveInput input, IDiagnosticsLogger logger)
        {
            throw new System.NotImplementedException();
        }
    }
}
