// <copyright file="MockBillingSubmissionCloudStorageFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    public class MockBillingSubmissionCloudStorageFactory : IBillingSubmissionCloudStorageFactory
    {
        public Task<IBillingSubmissionCloudStorageClient> CreateBillingSubmissionCloudStorage(AzureLocation location)
        {
            return Task.FromResult(new MockBillingSubmissionCloudStorageClient() as IBillingSubmissionCloudStorageClient);
        }
    }
}
