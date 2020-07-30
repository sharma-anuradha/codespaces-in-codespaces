// <copyright file="MockPartnerCloudStorageFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Factory used to generate per location storage clients.
    /// </summary>
    public class MockPartnerCloudStorageFactory : IPartnerCloudStorageFactory
    {
        /// <inheritdoc/>
        public Task<IPartnerCloudStorageClient> CreatePartnerCloudStorage(AzureLocation location, string partnerId)
        {
            return Task.FromResult(new MockPartnerCloudStorageClient() as IPartnerCloudStorageClient);
        }
    }
}
