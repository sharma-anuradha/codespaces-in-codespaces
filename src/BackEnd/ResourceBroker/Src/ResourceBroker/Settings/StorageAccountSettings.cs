// <copyright file="StorageAccountSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings
{
    /// <summary>
    /// Settings for the resource broker to use at runtime.
    /// </summary>
    public class StorageAccountSettings
    {
        /// <summary>
        /// Gets or sets the storage account name.
        /// TODO: Handle multiple regions!
        /// </summary>
        public string StorageAccountName { get; set; }

        /// <summary>
        /// Gets or sets the storage account key.
        /// TODO: Handle multiple regions!
        /// </summary>
        public string StorageAccountKey { get; set; }
    }
}
