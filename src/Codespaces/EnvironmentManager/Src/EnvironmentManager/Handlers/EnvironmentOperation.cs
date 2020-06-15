// <copyright file="EnvironmentOperation.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    /// <summary>
    /// Resource Operation.
    /// </summary>
    public enum EnvironmentOperation
    {
        /// <summary>
        /// Archiving operation.
        /// </summary>
        Archiving,

        /// <summary>
        /// Create Environment operation.
        /// </summary>
        Provisioning,

        /// <summary>
        /// Resuming Environment operation.
        /// </summary>
        Resuming,

        /// <summary>
        /// Shutting down environment operation.
        /// </summary>
        ShuttingDown,
    }
}
