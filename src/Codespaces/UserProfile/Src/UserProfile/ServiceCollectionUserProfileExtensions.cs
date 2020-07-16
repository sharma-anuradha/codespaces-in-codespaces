// <copyright file="ServiceCollectionUserProfileExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Contracts;
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
                .AddProfileHttpClient(configureUserProfileOptions)
                .AddIdentityContextAccessor();
        }

        private static IServiceCollection AddCurrentUserProvider(
            this IServiceCollection services)
            => services.AddSingleton<HttpContextCurrentUserProvider>()
                       .AddSingleton<ICurrentUserProvider>(x => x.GetRequiredService<HttpContextCurrentUserProvider>())
                       .AddSingleton<ICurrentIdentityProvider>(x => x.GetRequiredService<HttpContextCurrentUserProvider>());

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

        private static IServiceCollection AddIdentityContextAccessor(
            this IServiceCollection services)
            => services.AddSingleton<IIdentityContextAccessor, IdentityContextAccessor>()
                       .AddSingleton(sp =>
                                {
                                    var claims = new Claim[] { new Claim("Name", "Superuser") };
                                    var claimsIdentity = new ClaimsIdentity(claims);
                                    var authorizedScopes = PlanAccessTokenScopes.ValidPlanScopes;

                                    return new VsoSuperuserClaimsIdentity(authorizedScopes.ToArray(), claimsIdentity);
                                });
    }
}
