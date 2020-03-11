// <copyright file="ApplicationBuilderExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Builder;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Middleware
{
    /// <summary>
    /// <see cref="IApplicationBuilder"/> extensions for middleware.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="ConnectionCreationMiddleware"/>.
        /// </summary>
        /// <param name="builder">application builder.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder UseConnectionCreation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ConnectionCreationMiddleware>();
        }
    }
}
