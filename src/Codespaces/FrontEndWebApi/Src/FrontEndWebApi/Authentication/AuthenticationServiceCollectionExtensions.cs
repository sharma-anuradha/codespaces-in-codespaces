// <copyright file="AuthenticationServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using StackExchange.Redis;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for authentication.
    /// </summary>
    public static class AuthenticationServiceCollectionExtensions
    {
        private const string DataProtectionName = ServiceConstants.ServiceName;
        private const string DataProtectionRedisKey = "DataProtection-Keys";

        /// <summary>
        /// Add handler for a validated principal.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddValidatedPrincipalIdentityHandler(
            this IServiceCollection services)
        {
            services.TryAddSingleton<IValidatedPrincipalIdentityHandler, ValidatedPrincipalIdentityHandler>();
            return services;
        }

        /// <summary>
        /// Add Authentication.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="hostEnvironment">The aspnet host environment.</param>
        /// <param name="redisCacheOptions">The redis cache options.</param>
        /// <param name="settings">The RPaaS settings.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddCustomFrontEndAuthentication(
            this IServiceCollection services,
            IWebHostEnvironment hostEnvironment,
            RedisCacheOptions redisCacheOptions,
            RPaaSSettings settings)
        {
            Requires.NotNull(hostEnvironment, nameof(hostEnvironment));
            Requires.NotNull(redisCacheOptions, nameof(redisCacheOptions));
            Requires.NotNull(settings, nameof(settings));

            services.AddVsSaaSCoreDataProtection(hostEnvironment, redisCacheOptions.RedisConnectionString);

            services
                .AddAuthentication()
                .AddRPaaSJwtBearer(settings)
                .AddVMTokenJwtBearer()
                .AddVsSaaSCookieBearer();

            return services;
        }

        private static void AddVsSaaSCoreDataProtection(
            this IServiceCollection services,
            IWebHostEnvironment hostEnvironment,
            string redisConnectionString)
        {
            var dataProtection = services
                .AddDataProtection()
                .SetApplicationName(DataProtectionName);

            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                if (!hostEnvironment.IsDevelopment())
                {
                    // TODO: Need to make sure that keys are persisted, etc
                    var redis = ConnectionMultiplexer.Connect(redisConnectionString);
                    dataProtection.PersistKeysToStackExchangeRedis(redis, DataProtectionRedisKey);
                }
            }
        }
    }
}
