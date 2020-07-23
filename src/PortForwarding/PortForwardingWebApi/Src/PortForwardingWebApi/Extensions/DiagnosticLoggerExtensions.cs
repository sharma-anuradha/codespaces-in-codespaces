// <copyright file="DiagnosticLoggerExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IDiagnosticsLogger"/>.
    /// </summary>
    public static class DiagnosticLoggerExtensions
    {
        /// <summary>
        /// Adds basic request authentication values to the logger.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="context">The authentication result context.</param>
        /// <returns>The logger with additional values.</returns>
        public static IDiagnosticsLogger AddAuthenticationResultContext(this IDiagnosticsLogger logger, ResultContext<JwtBearerOptions> context)
        {
            return logger
                .FluentAddValue("Scheme", context.Scheme.Name)
                .FluentAddValue("Audience", context.Options.Audience)
                .FluentAddValue("Authority", context.Options.Authority)
                .FluentAddValue("HttpRequestUri", context.Request.GetDisplayUrl())
                .FluentAddValue("PrincipalIsAuthenticationType", context.Principal?.Identity.AuthenticationType)
                .FluentAddValue("PrincipalIsAuthenticated", context.Principal?.Identity.IsAuthenticated);
        }
    }
}
