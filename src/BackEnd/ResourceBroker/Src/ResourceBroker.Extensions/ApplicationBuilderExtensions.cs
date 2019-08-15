// <copyright file="ApplicationBuilderExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Mocks;

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
        /// <param name="env">The hosting environment.</param>
        /// <returns>The applicaton builder instance.</returns>
        public static IApplicationBuilder UseResourceBroker(
            this IApplicationBuilder app, IHostingEnvironment env)
        {
            Requires.NotNull(app, nameof(app));

            if (env.IsDevelopment())
            {
                var resourceBroker = app.ApplicationServices.GetService(typeof(IResourceBroker)) as IResourceBroker;
                if (!(resourceBroker is MockResourceBroker))
                {
                    // Setup dashboard
                    app.UseHangfireDashboard();
                }
            }

            return app;
        }
    }
}
