// <copyright file="JobErrorType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Job error type.
    /// </summary>
    public enum JobErrorType
    {
        /// <summary>
        /// System or Internal error.
        /// </summary>
        System = 0,

        /// <summary>
        /// External user error.
        /// </summary>
        User = 1,
    }
}
