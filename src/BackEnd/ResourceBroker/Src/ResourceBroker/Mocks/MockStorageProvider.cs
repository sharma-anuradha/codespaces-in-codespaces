// <copyright file="MockStorageProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Mocks
{
    /// <summary>
    /// 
    /// </summary>
    public class MockStorageProvider : IStorageProvider
    {
        private Random Random { get; } = new Random();

        public async Task<FileShareProviderAssignResult> AssignAsync(FileShareProviderAssignInput input, string continuationToken = null)
        {
            await Task.Delay(Random.Next(100, 1000));

            return new FileShareProviderAssignResult("AccountName", "AccountKey", "ShareName", "FileName");
        }

        public Task<FileShareProviderCreateResult> CreateAsync(FileShareProviderCreateInput input, string continuationToken = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<FileShareProviderDeleteResult> DeleteAsync(FileShareProviderDeleteInput input, string continuationToken = null)
        {
            throw new System.NotImplementedException();
        }
    }
}
