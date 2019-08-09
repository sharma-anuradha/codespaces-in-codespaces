// <copyright file="AzureClientException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models
{
    /// <summary>
    /// Exception of errors while attempting to get an Azure client.
    /// </summary>
    public class AzureClientException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureClientException"/> class.
        /// </summary>
        /// <param name="azureSubscriptionId">Azure subscription id.</param>
        public AzureClientException(string azureSubscriptionId)
            : base(string.Format("Failed to create Azure client: {0}", azureSubscriptionId))
        {
        }
    }
}
