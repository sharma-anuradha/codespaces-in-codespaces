// <copyright file="IBillingSubmissionCloudStorageFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Interface for the bill submission cloud storage factories.
    /// </summary>
    public interface IBillingSubmissionCloudStorageFactory
    {
        /// <summary>
        /// Generates a billing submission cloud storage client wired up to the correct table for the given location.
        /// </summary>
        /// <param name="location">The desired data plane location.</param>
        /// <returns>A billing submission cloud storage client.</returns>
        Task<IBillingSubmissionCloudStorageClient> CreateBillingSubmissionCloudStorage(AzureLocation location);
    }
}
