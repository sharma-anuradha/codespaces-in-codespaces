// <copyright file="ResourceProviderErrorResponse.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    /// <summary>
    /// The error class used by the ResourceProvider to send properly formatted responses back to RPaaS.
    /// </summary>
    public class ResourceProviderErrorResponse
    {
        /// <summary>
        /// Gets or sets the status of the request.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the error object.
        /// </summary>
        public ResourceProviderErrorInfo Error { get; set; }
    }
}
