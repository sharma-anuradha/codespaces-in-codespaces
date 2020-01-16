// <copyright file="ArchiveStorageProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.ArchiveStorageProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ArchiveStorageProvider
{
    /// <inheritdoc/>
    public class ArchiveStorageProvider : IArchiveStorageProvider
    {
        private static readonly StubArchiveStorageInfo Stub = new StubArchiveStorageInfo();

        /// <inheritdoc/>
        public Task<IArchiveStorageInfo> GetArchiveStorageAccountAsync(AzureLocation azureLocation, int minimumRequiredGB)
        {
            return Task.FromResult<IArchiveStorageInfo>(Stub);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<IArchiveStorageInfo>> ListArchiveStorageAccountsAsync(AzureLocation azureLocation)
        {
            var stub = new StubArchiveStorageInfo();
            return Task.FromResult<IEnumerable<IArchiveStorageInfo>>(new[] { Stub });
        }

        private class StubArchiveStorageInfo : IArchiveStorageInfo
        {
            public IAzureResourceLocation AzureResourceLocation => throw new NotImplementedException();

            public string StorageAccountName => throw new NotImplementedException();
        }
    }
}
