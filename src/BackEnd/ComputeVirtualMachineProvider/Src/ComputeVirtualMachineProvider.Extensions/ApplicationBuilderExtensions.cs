// <copyright file="ApplicationBuilderExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Extensions
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
        public static IApplicationBuilder UseComputeVirtualMachineProvider(
            this IApplicationBuilder app, IHostingEnvironment env)
        {
            Requires.NotNull(app, nameof(app));

            // Nothing yet

            return app;
        }
    }
}
