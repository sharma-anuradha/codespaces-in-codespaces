// <copyright file="EnvironmentType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// The environment type.
    /// </summary>
    public enum EnvironmentType
    {
        /// <summary>
        /// A Cloud Environment (default)
        /// </summary>
        CloudEnvironment = 0,

        /// <summary>
        /// A self-hosted environment
        /// </summary>
        StaticEnvironment = 1,
    }
}
