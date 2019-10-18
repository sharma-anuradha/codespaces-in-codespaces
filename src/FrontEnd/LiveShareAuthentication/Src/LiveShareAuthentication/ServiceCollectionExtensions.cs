// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareAuthentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LiveshareAuthentication
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for the Environment Manager.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the Live Share authentication provider.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureAuthHttpClientProviderOptions">Configuration options.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddLiveshareAuthProvider(
            this IServiceCollection services,
            Action<AuthHttpClientProviderOptions> configureAuthHttpClientProviderOptions)
        {
            services
                .Configure(configureAuthHttpClientProviderOptions)
                .AddSingleton<ICurrentUserHttpClientProvider<AuthHttpClientProviderOptions>, AuthHttpClientProvider>();

            services.AddSingleton<IAuthRepository, HttpClientAuthRepository>();

            return services;
        }
    }
}
