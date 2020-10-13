// <copyright file="ITokenScopeHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.TokenService.Scopes
{
    /// <summary>
    /// A service that authorizes an identity and transforms a token payload for given a scope.
    /// </summary>
    public interface ITokenScopeHandler
    {
        /// <summary>
        /// Transforms a token payload, given a requested scope. The transform may perform
        /// additional access checks against the identity claims.
        /// </summary>
        /// <param name="identity">Authenticated identity built from the client's auth
        /// token.</param>
        /// <param name="payload">Payload to be transformed by this method.</param>
        /// <param name="scope">The scope that the client has requested. If the scope is
        /// unknown to this transform, this method should do nothing and complete successfully.
        /// (It will be handled by a different transform.)</param>
        /// <param name="logger">Diagnostic logger.</param>
        /// <returns>True if this handler handled the scope.</returns>
        /// <exception cref="ArgumentException">Some identity claims are missing or
        /// invalid for the requested scope.</exception>
        /// <exception cref="UnauthorizedAccessException">The identity is not authorized to
        /// get a token with the requested scope.</exception>
        Task<bool> TransformPayloadForScopeAsync(
            ClaimsIdentity identity,
            JwtPayload payload,
            string scope,
            IDiagnosticsLogger logger);
    }
}
