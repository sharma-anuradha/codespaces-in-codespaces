// <copyright file="ErrorCodes.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// List error codes returned by <see cref="ICloudEnvironmentManager"/>.
    /// </summary>
    public enum ErrorCodes
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Quota exceeded.
        /// </summary>
        ExceededQuota = 1,

        /// <summary>
        /// Environment name specified already exists.
        /// </summary>
        EnvironmentNameAlreadyExists = 2,

        /// <summary>
        /// Cannot find the requested environment.
        /// </summary>
        EnvironmentDoesNotExist = 3,

        /// <summary>
        /// Cannot shutdown a static environment.
        /// </summary>
        ShutdownStaticEnvironment = 4,

        /// <summarys
        /// Cannot start a static environment.
        /// </summary>
        StartStaticEnvironment = 5,

        /// <summary>
        /// Environment is not available.
        /// </summary>
        EnvironmentNotAvailable = 6,

        /// <summary>
        /// Environment is not shutdown.
        /// </summary>
        EnvironmentNotShutdown = 7,

        /// <summary>
        /// Unable to allocate Storage or Compute resource from the pools.
        /// </summary>
        UnableToAllocateResources = 8,
    }
}
