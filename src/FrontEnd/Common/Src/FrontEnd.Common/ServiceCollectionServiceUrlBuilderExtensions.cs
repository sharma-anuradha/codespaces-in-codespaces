// <copyright file="ServiceCollectionServiceUrlBuilderExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEnd.Common
{
    /// <summary>
    /// Configure and add service uri builder.
    /// </summary>
    public static class ServiceCollectionServiceUrlBuilderExtensions
    {
        /// <summary>
        /// Configure and add service uri builder and dependencies.
        /// </summary>
        /// <param name="services">list of services.</param>
        /// <param name="forwardingHost">host to forward the calls to. Null or empty to use the default.</param>
        /// <returns>services.</returns>
        public static IServiceCollection AddServiceUriBuilder(this IServiceCollection services, string forwardingHost)
        {
            var developerSettings = !string.IsNullOrWhiteSpace(forwardingHost);
            services.AddSingleton(new DeveloperSettings(developerSettings, forwardingHost));
            services.AddSingleton<IServiceUriBuilder, ServiceUriBuilder>();
            return services;
        }
    }
}