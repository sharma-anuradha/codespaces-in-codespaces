// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Extensions
{
    /// <summary>
    /// Extensions methods for <see cref="IServiceCollection"/> related to the Token creation.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds VM token provider to the collection of services.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="appSettings">App settings.</param>
        /// <returns>Adds VMToken povider to the list of services.</returns>
        public static IServiceCollection AddVMTokenProvider(
            this IServiceCollection services,
            AppSettings appSettings)
        {
            Requires.NotNull(services, nameof(services));
            Requires.NotNull(appSettings, nameof(appSettings));

            // Short circuit things if Token Providers is being mocked
            if (appSettings.UseMocksForTokenProviders)
            {
                return services;
            }

            // Core services
            services.AddSingleton<IVSSaaSTokenProvider, VMTokenProvider>();

            return services;
        }
    }
}
