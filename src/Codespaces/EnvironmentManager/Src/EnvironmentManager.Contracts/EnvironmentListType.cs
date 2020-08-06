// <copyright file="EnvironmentListType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// The enum delete parameter for list action so that action know if the request is for
    /// active/deleted/all environments.
    /// </summary>
    public enum EnvironmentListType
    {
        /// <summary>
        /// Only active environments
        /// </summary>
        ActiveEnvironments = 0,

        /// <summary>
        /// Only deleted environments
        /// </summary>
        DeletedEnvironments = 1,

        /// <summary>
        /// Active and deleted environments.
        /// </summary>
        AllEnvironments = 2,
    }
}
