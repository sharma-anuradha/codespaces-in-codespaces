// <copyright file="ApplicationBuilderExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Builder;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions
{
    /// <summary>
    /// Extensions methods for <see cref="IApplicationBuilder"/> related to the system catalog.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Uses and validates the system catalog.
        /// </summary>
        /// <param name="app">The aspnet core application builder.</param>
        /// <returns>The applicaton builder instance.</returns>
        public static IApplicationBuilder UseResourceBroker(
            this IApplicationBuilder app)
        {
            Requires.NotNull(app, nameof(app));

            // Nothing currently

            return app;
        }
    }
}
