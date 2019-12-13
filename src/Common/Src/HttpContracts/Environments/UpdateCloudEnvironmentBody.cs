// <copyright file="UpdateCloudEnvironmentBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments
{
    /// <summary>
    /// The REST API body for updating an existing Environment.
    /// </summary>
    public class UpdateCloudEnvironmentBody
    {
        /// <summary>
        /// Gets or sets the cloud environment sku name.
        /// </summary>
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets the auto shutdown time the user specified.
        /// </summary>
        public int? AutoShutdownDelayMinutes { get; set; }
    }
}