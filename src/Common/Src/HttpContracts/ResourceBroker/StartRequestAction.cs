// <copyright file="StartRequestAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker
{
    /// <summary>
    /// Start Request Operation.
    /// </summary>
    public enum StartRequestAction
    {
        /// <summary>
        /// Indicates that the compute should be started.
        /// </summary>
        StartCompute = 1,

        /// <summary>
        /// Indicates that the blob storage should be archived.
        /// </summary>
        StartArchive = 2,

        /// <summary>
        /// Indicates that the compute should be started and exported.
        /// </summary>
        StartExport = 3,

        /// <summary>
        /// Indicates that the compute should be started for updating.
        /// </summary>
        StartUpdate = 4,
    }
}
