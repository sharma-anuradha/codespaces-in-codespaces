// <copyright file="MockPartnerCloudStorageClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Mock class for the bill submission client.
    /// </summary>
    public class MockPartnerCloudStorageClient : IPartnerCloudStorageClient
    {
        /// <summary>
        /// Push partner queue submission.
        /// </summary>
        /// <param name="gitHubQueueSubmission">The partner queue submission.</param>
        /// <returns>Returns a task to await the push.</returns>
        public Task PushPartnerQueueSubmission(PartnerQueueSubmission gitHubQueueSubmission)
        {
            throw new System.NotImplementedException();
        }
    }
}
