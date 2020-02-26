// <copyright file="ClaimsPrincipalExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Security.Claims;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="ClaimsPrincipal"/>.
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Constructs the current user id from principal's claims.
        /// </summary>
        /// <param name="principal">The <see cref="ClaimsPrincipal"/>.</param>
        /// <returns>The user id or null if the correct claims aren't provided.</returns>
        public static string GetUserIdFromClaims(this ClaimsPrincipal principal)
        {
            var tid = principal.FindFirstValue(CustomClaims.TenantId);
            var oid = principal.FindFirstValue(CustomClaims.OId);

            if (string.IsNullOrEmpty(tid) || string.IsNullOrEmpty(oid))
            {
                return null;
            }

            return $"{tid}_{oid}";
        }
    }
}
