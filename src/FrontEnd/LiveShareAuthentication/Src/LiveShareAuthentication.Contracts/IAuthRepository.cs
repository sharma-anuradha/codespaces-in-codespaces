// <copyright file="IAuthRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareAuthentication
{
    /// <summary>
    /// Liveshare Exchange token Repository.
    /// </summary>
    public interface IAuthRepository
    {
        /// <summary>
        /// Exchange an AAD token for a Cascade token.
        /// </summary>
        /// <param name="externalToken">The AAD token.</param>
        /// <returns>The Cascade Token.</returns>
        Task<string> ExchangeToken(string externalToken);
    }
}
