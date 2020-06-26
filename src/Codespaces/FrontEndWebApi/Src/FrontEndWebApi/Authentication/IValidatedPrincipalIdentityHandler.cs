// <copyright file="IValidatedPrincipalIdentityHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common.Identity;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication
{
    /// <summary>
    /// Handle user identities for a validated principal.
    /// </summary>
    public interface IValidatedPrincipalIdentityHandler
    {
        /// <summary>
        /// Handle user identities for a validated principal.
        /// Returns with no exception if the principal was valid and properly handled. Otherwise, throws <see cref="IdentityValidationException"/>.
        /// </summary>
        /// <param name="principal">The principal.</param>
        /// <param name="token">The JWT security token (JWT only, null for Cookie).</param>
        /// <param name="logger">The request logger.</param>
        /// <returns>An async task.</returns>
        /// <exception cref="IdentityValidationException">The principal identity was invalid or could not be handled.</exception>
        Task<ClaimsPrincipal> ValidatedPrincipalAsync(ClaimsPrincipal principal, JwtSecurityToken token, IDiagnosticsLogger logger);
    }
}