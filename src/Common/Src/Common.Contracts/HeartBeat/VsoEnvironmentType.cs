// <copyright file="VsoEnvironmentType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Represents different types of cloud environment deployments.
    /// </summary>
    public enum VsoEnvironmentType
    {
        /// <summary>
        /// Environment running inside a docker container.
        /// </summary>
        ContainerBased,

        /// <summary>
        /// Environment running directly on VM (for example Windows environments)
        /// </summary>
        VirtualMachineBased,
    }
}