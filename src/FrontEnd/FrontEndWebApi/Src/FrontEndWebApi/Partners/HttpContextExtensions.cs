// <copyright file="HttpContextExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Partners
{
    /// <summary>
    /// Extension methods for <see cref="HttpContext"/> related to partner integrations.
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Gets the <see cref="Partner"/> making the current request by Service Principal id, or null if the request is not from a known partner.
        /// </summary>
        /// <param name="context">The request context.</param>
        /// <param name="config">The system configuration.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The current <see cref="Partner"/> or null.</returns>
        public static async Task<Partner?> GetPartnerAsync(this HttpContext context, ISystemConfiguration config, IDiagnosticsLogger logger)
        {
            var currentUser = context.User.GetUserIdFromClaims();

            if (string.IsNullOrEmpty(currentUser))
            {
                return null;
            }

            return await config.GetUserIdValueAsync<Partner?>("partners:partner-for-user", currentUser, logger, null);
        }
    }
}
