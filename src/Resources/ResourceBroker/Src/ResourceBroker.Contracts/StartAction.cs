// <copyright file="StartAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts
{
    /// <summary>
    /// Start Action.
    /// </summary>
    public enum StartAction
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
        /// Indicates that the compute should be started and environment should be exported.
        /// </summary>
        StartExport = 3,

        /// <summary>
        /// Indicates that the compute should be started for updating.
        /// </summary>
        StartUpdate = 4,
    }
}
