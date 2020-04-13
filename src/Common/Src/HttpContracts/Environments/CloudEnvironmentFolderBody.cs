// <copyright file="CloudEnvironmentFolderBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Environments
{
    /// <summary>
    /// The REST API body for updating the list of recent folders.
    /// </summary>
    public class CloudEnvironmentFolderBody
    {
        /// <summary>
        /// Gets or sets the folder path.
        /// </summary>
        public List<string> RecentFolderPaths { get; set; }
    }
}
