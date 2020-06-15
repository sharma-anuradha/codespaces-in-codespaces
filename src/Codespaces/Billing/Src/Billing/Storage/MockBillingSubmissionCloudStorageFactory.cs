// <copyright file="MockBillingSubmissionCloudStorageFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Factory used to generate per location storage clients.
    /// </summary>
    public class MockBillingSubmissionCloudStorageFactory : IBillingSubmissionCloudStorageFactory
    {
        /// <inheritdoc/>
        public Task<IBillingSubmissionCloudStorageClient> CreateBillingSubmissionCloudStorage(AzureLocation location)
        {
            return Task.FromResult(new MockBillingSubmissionCloudStorageClient() as IBillingSubmissionCloudStorageClient);
        }
    }
}
