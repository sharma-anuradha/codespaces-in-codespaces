// <copyright file="ICascadeTokenReader.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication
{
    /// <summary>
    /// Interface for reading cascade tokens.
    /// </summary>
    public interface ICascadeTokenReader
    {
        /// <summary>
        /// Validates a Cascade token and returns a ClaimsPrincipal constructed from the token claims.
        /// </summary>
        /// <param name="accessToken">The JWT security token (JWT only, null for Cookie).</param>
        /// <param name="logger">The request logger.</param>
        /// <returns>ClaimsPrincipal constructed from the token claims.</returns>
        /// <exception cref="SecurityTokenException">The token was invalid.</exception>
        ClaimsPrincipal ReadTokenPrincipal(string accessToken, IDiagnosticsLogger logger);
    }
}