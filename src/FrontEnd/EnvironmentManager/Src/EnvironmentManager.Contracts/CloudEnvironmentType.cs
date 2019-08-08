// <copyright file="CloudEnvironmentType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// The environment type.
    /// </summary>
    public enum CloudEnvironmentType
    {
        /// <summary>
        /// A Cloud Environment (default)
        /// </summary>
        CloudEnvironment = 0,

        /// <summary>
        /// A static environment (internal use)
        /// </summary>
        StaticEnvironment = 1,
    }
}
