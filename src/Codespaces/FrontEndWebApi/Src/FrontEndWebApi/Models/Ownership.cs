// <copyright file="Ownership.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    /// <summary>
    /// Indicates ownership status of a plan or codespace resource relative to the current user.
    /// </summary>
    public enum Ownership
    {
        /// <summary>
        /// The resource is not shared, but ownership cannot be determined because the current
        /// identity info is missing or incomplete.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The resource is not owned by any user; it is shared, subject to Azure RBAC permissions.
        /// </summary>
        Shared = 1,

        /// <summary>
        /// The resource is owned by the current user.
        /// </summary>
        CurrentUser = 2,

        /// <summary>
        /// The resource is owned by another user.
        /// </summary>
        OtherUser = 3,
    }
}
