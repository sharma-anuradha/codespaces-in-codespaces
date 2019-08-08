// <copyright file="IProfileRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile
{
    /// <summary>
    /// A profile repository.
    /// </summary>
    public interface IProfileRepository
    {
        /// <summary>
        /// Get the user profile for the current user.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The current user's profile.</returns>
        Task<Profile> GetCurrentUserProfileAsync(IDiagnosticsLogger logger);
    }
}
