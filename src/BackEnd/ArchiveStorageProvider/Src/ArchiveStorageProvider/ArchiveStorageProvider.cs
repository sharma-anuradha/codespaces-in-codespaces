// <copyright file="ArchiveStorageProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.ArchiveStorageProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ArchiveStorageProvider
{
    /// <inheritdoc/>
    public class ArchiveStorageProvider : IArchiveStorageProvider
    {
        private static readonly StubArchiveStorageInfo Stub = new StubArchiveStorageInfo();

        /// <inheritdoc/>
        public Task<IArchiveStorageInfo> GetArchiveStorageAccountAsync(AzureLocation azureLocation, int minimumRequiredGB, IDiagnosticsLogger logger, bool forceCapacityCheck = false)
        {
            return Task.FromResult<IArchiveStorageInfo>(Stub);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<IArchiveStorageInfo>> ListArchiveStorageAccountsAsync(AzureLocation azureLocation, IDiagnosticsLogger logger)
        {
            var stub = new StubArchiveStorageInfo();
            return Task.FromResult<IEnumerable<IArchiveStorageInfo>>(new[] { Stub });
        }

        private class StubArchiveStorageInfo : IArchiveStorageInfo
        {
            public AzureResourceInfo AzureResourceInfo => throw new NotImplementedException();

            public string StorageAccountKey => throw new NotImplementedException();
        }
    }
}
