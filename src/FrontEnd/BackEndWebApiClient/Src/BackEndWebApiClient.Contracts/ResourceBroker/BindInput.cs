// <copyright file="BindInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker
{
    /// <summary>
    /// A request to bind compute to storage.
    /// </summary>
    public class BindInput
    {
        /// <summary>
        /// Gets or sets the compute resource id token.
        /// </summary>
        public string ComputeResourceIdToken { get; set; }

        /// <summary>
        /// Gets or sets the storage resource id token.
        /// </summary>
        public string StorageResourceIdToken { get; set; }
    }
}
