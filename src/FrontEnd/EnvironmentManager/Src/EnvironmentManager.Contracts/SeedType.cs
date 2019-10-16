// <copyright file="SeedType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// The seed type.
    /// </summary>
    public enum SeedType
    {
        /// <summary>
        /// The seed is a Git repository.
        /// </summary>
        Git = 0,

        /// <summary>
        /// The seed is whatever SVN means.
        /// </summary>
        Svn = 1,

        /// <summary>
        /// The seed is StaticEnvironment.
        /// </summary>
        StaticEnvironment = 2,
    }
}
