// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ManagedIdentityProvider
{
    /// <summary>
    /// Extensions methods for <see cref="IServiceCollection"/> related to managed identities.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// <see cref="IServiceCollection"/> extensions for the compute-virtual-machine provider.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddManagedIdentityProvider(this IServiceCollection services)
        {
            Requires.NotNull(services, nameof(services));

            services.AddSingleton<IManagedIdentityHttpClientProvider, ManagedIdentityHttpClientProvider>();
            services.AddSingleton<IManagedIdentityProvider, ManagedIdentityProvider>();

            services.AddSingleton<ISharedIdentityHttpClientProvider, SharedIdentityHttpClientProvider>();
            services.AddSingleton<ISharedIdentitiesProvider, SharedIdentitiesProvider>();

            return services;
        }
    }
}
