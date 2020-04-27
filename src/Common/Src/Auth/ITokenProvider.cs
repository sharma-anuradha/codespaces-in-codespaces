// <copyright file="ITokenProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Auth
{
    /// <summary>
    /// Interface for creating tokens from global <see cref="AuthenticationSettings"/>.
    /// </summary>
    /// <remarks>
    /// Usage of this interface should be primarily driven by extension methods.
    /// </remarks>
    public interface ITokenProvider
    {
        /// <summary>
        /// Gets the authentication settings.
        /// </summary>
        AuthenticationSettings Settings { get; }

        /// <summary>
        /// Issues a new JWT token.
        /// </summary>
        /// <param name="issuer">Token issuer URI (required).</param>
        /// <param name="audience">Token audience URI (required).</param>
        /// <param name="expires">UTC date the token expires (required).</param>
        /// <param name="claims">Additional claims for the token to be issued.</param>
        /// <param name="logger">Diagnostic logger.</param>
        /// <returns>The issued JWT token.</returns>
        /// <exception cref="ArgumentException">Some claims were missing or invalid.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller is not authorized to issue
        /// tokens with the requested claims.</exception>
        Task<string> IssueTokenAsync(
            string issuer,
            string audience,
            DateTime expires,
            IEnumerable<Claim> claims,
            IDiagnosticsLogger logger);
    }
}
