// <copyright file="IIdentityContextAccessor.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile
{
    /// <summary>
    /// Provides access to <see cref="IdentityContext"/> if one is available.
    /// </summary>
    /// <remarks>
    /// This interface should be used with caution. It relies on <see cref="AsyncLocal{T}" /> which can have a negative performance impact on async calls.
    /// It also creates a dependency on "ambient state" which can make testing more difficult.
    /// </remarks>
    public interface IIdentityContextAccessor
    {
        /// <summary>
        /// Gets or sets current identity context.
        /// </summary>
        IdentityContext IdentityContext { get; set; }
    }
}
