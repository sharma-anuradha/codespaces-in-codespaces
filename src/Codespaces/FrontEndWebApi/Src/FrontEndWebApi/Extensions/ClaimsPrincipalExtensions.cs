// <copyright file="ClaimsPrincipalExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Linq;
using System.Security.Claims;
using Microsoft.VsSaaS.Common.Identity;

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
            var identity = principal.Identities.First();
            var tid = identity.GetTenantId();
            var oid = identity.GetObjectId();
            if (string.IsNullOrEmpty(tid) || string.IsNullOrEmpty(oid))
            {
                return null;
            }

            return $"{tid}_{oid}";
        }
    }
}
