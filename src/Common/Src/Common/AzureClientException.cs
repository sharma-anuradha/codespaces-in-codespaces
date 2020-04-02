// <copyright file="AzureClientException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
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
        /// <param name="ex">Operation exception.</param>
        public AzureClientException(string azureSubscriptionId, Exception ex)
            : base(string.Format("Failed to create Azure client for subscription: {0}", azureSubscriptionId), ex)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureClientException"/> class.
        /// </summary>
        /// <param name="azureSubscriptionId">Azure subscription id.</param>
        /// <param name="ex">Operation exception.</param>
        public AzureClientException(Guid azureSubscriptionId, Exception ex)
            : base(string.Format("Failed to create Azure client for subscription: {0}", azureSubscriptionId), ex)
        {
        }
    }
}
