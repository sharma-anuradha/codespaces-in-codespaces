// <copyright file="IPartnerCloudStorageClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// An interface that represents the cloud storage client.
    /// </summary>
    public interface IPartnerCloudStorageClient
    {
        /// <summary>
        /// Pushes a Partner invoice to the GitHub queue.
        /// </summary>
        /// <param name="queueSubmission">the github invoice to submit.</param>
        /// <returns>a task that completed when the push completes.</returns>
        Task PushPartnerQueueSubmission(PartnerQueueSubmission queueSubmission);
    }
}
