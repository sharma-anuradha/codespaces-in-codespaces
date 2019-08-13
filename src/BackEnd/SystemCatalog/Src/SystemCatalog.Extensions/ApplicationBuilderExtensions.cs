﻿// <copyright file="ApplicationBuilderExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Extensions
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
        /// <param name="env">The hosting environment.</param>
        /// <returns>The applicaton builder instance.</returns>
        public static IApplicationBuilder UseSystemCatalog(
            this IApplicationBuilder app, IHostingEnvironment env)
        {
            Requires.NotNull(app, nameof(app));

            try
            {
                _ = app.ApplicationServices.GetService<ISystemCatalog>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"The system catalog is not registered or has malformed configuration: {ex.Message}", ex);
            }

            return app;
        }
    }
}
