// <copyright file="ExportType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Environments
{
    /// <summary>
    /// Possible types of export operations.
    /// </summary>
    public enum ExportType
    {
        /// <summary>
        /// Zips up and uploads the files on the users workspace directory.
        /// </summary>
        Workspace = 1,

        /// <summary>
        /// Creates a new branch on the users main repository and pushes the pending changes to it.
        /// </summary>
        GitPush = 2,
    }
}
