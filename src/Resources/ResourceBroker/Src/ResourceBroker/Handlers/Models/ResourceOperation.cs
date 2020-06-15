// <copyright file="ResourceOperation.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models
{
    /// <summary>
    /// States of resources.
    /// </summary>
    public enum ResourceOperation
    {
        /// <summary>
        /// Represents deleting operation.
        /// </summary>
        Deleting,

        /// <summary>
        /// Represents start environment operation.
        /// </summary>
        StartEnvironment,

        /// <summary>
        /// Represents provisioning operation.
        /// </summary>
        Provisioning,

        /// <summary>
        /// Represents cleanup operation.
        /// </summary>
        CleanUp,

        /// <summary>
        /// Represents start archiving operation.
        /// </summary>
        StartArchiving,
    }
}
