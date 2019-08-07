// <copyright file="StorageAccountOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings
{
    /// <summary>
    /// An options object for the Azure storage account settings.
    /// </summary>
    public class StorageAccountOptions
    {
        /// <summary>
        /// Gets or sets the Azure storage account settings.
        /// </summary>
        public StorageAccountSettings Settings { get; set; } = new StorageAccountSettings();
    }
}
