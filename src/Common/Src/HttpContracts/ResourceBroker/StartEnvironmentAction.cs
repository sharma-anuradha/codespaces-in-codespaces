// <copyright file="StartEnvironmentAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker
{
    /// <summary>
    /// Start Request Operation.
    /// </summary>
    public enum StartEnvironmentAction
    {
        /// <summary>
        /// Indicates that environment should be resumed, created, or archived.
        /// </summary>
        StartCompute = 1,

        /// <summary>
        /// Indicates that the compute should be started and exported.
        /// </summary>
        StartExport = 2,

        /// <summary>
        /// Indicates that the compute should be started and updated.
        /// </summary>
        StartUpdate = 3,
    }
}
