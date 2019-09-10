// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore.Extensions
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for <see cref="IServicePrincipal"/> and <see cref="IControlPlaneAzureResourceAccessor"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the global/application <see cref="IServicePrincipal"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="servicePrincipalSettings">The application service principal settings.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddApplicationServicePrincipal(
            this IServiceCollection services,
            ServicePrincipalSettings servicePrincipalSettings)
        {
            Requires.NotNull(services, nameof(services));
            Requires.NotNull(servicePrincipalSettings, nameof(servicePrincipalSettings));

            services.Configure<ServicePrincipalOptions>(options =>
            {
                options.ServicePrincipalSettings = servicePrincipalSettings;
            });
            services.AddSingleton<IServicePrincipal, ServicePrincipal>();

            return services;
        }

        /// <summary>
        /// Adds the <see cref="ICurrentLocationProvider"/>.
        /// </summary>
        /// <param name="services">The service collecdtion.</param>
        /// <param name="azureLocation">The current azure location.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddCurrentLocationProvider(this IServiceCollection services, AzureLocation azureLocation)
        {
            return services.AddSingleton<ICurrentLocationProvider>(new CurrentLocationProvider(azureLocation));
        }

        /// <summary>
        /// Adds the <see cref="IControlPlaneInfo"/>.
        /// </summary>
        /// <param name="services">The service collecdtion.</param>
        /// <param name="controlPlaneSettings">The control plane settings.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddControlPlaneInfo(
            this IServiceCollection services,
            ControlPlaneSettings controlPlaneSettings)
        {
            services.Configure<ControlPlaneInfoOptions>(options =>
            {
                options.ControlPlaneSettings = controlPlaneSettings;
            });
            services.AddSingleton<IControlPlaneInfo, ControlPlaneInfo>();
            return services;
        }

        /// <summary>
        /// Adds the <see cref="IControlPlaneAzureResourceAccessor"/>.
        /// </summary>
        /// <param name="services">The service collecdtion.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddControlPlaneAzureResourceAccessor(
            this IServiceCollection services)
        {
            services.AddHttpClient<ControlPlaneAzureResourceAccessor.HttpClientWrapper>();
            return services.AddSingleton<IControlPlaneAzureResourceAccessor, ControlPlaneAzureResourceAccessor>();
        }
    }
}
