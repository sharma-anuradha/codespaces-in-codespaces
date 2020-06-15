// <copyright file="ServiceCollectionUserProfileExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Http;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for the user profile module.
    /// </summary>
    public static class ServiceCollectionUserProfileExtensions
    {
        /// <summary>
        /// Adds the user profile providers.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureUserProfileOptions">The options callback.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddUserProfile(
            this IServiceCollection services,
            Action<ProfileHttpClientProviderOptions> configureUserProfileOptions)
        {
            Requires.NotNull(services, nameof(services));
            Requires.NotNull(configureUserProfileOptions, nameof(configureUserProfileOptions));

            return services
                .AddCurrentUserProvider()
                .AddProfileCache()
                .AddProfileRepository()
                .AddProfileHttpClient(configureUserProfileOptions);
        }

        private static IServiceCollection AddCurrentUserProvider(
            this IServiceCollection services)
            => services.AddSingleton<ICurrentUserProvider, HttpContextCurrentUserProvider>();

        private static IServiceCollection AddProfileCache(
            this IServiceCollection services)
            => services.AddSingleton<IProfileCache, HttpContextProfileCache>();

        private static IServiceCollection AddProfileRepository(
            this IServiceCollection services)
            => services.AddSingleton<IProfileRepository, HttpClientProfileRepository>();

        private static IServiceCollection AddProfileHttpClient(
            this IServiceCollection services,
            Action<ProfileHttpClientProviderOptions> configureOptions)
            => services
                .Configure(configureOptions)
                .AddSingleton<IHttpClientProvider<ProfileHttpClientProviderOptions>,
                    CurrentUserHttpClientProvider<ProfileHttpClientProviderOptions>>();
    }
}
