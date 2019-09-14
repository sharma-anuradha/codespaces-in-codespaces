// <copyright file="AuthenticationServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
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
        /// Add Authentication.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="hostEnvironment">The aspnet host environment.</param>
        /// <param name="redisCacheOptions">The redis cache options.</param>
        /// <param name="jwtBearerOptions">The JWT bearer options.</param>
        /// <param name="rpSaasAuthority">The RPSaaS Signature Authority URL.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddVsSaaSAuthentication(
            this IServiceCollection services,
            IHostingEnvironment hostEnvironment,
            RedisCacheOptions redisCacheOptions,
            JwtBearerOptions jwtBearerOptions,
            string rpSaasAuthority)
        {
            Requires.NotNull(hostEnvironment, nameof(hostEnvironment));
            Requires.NotNull(redisCacheOptions, nameof(redisCacheOptions));
            Requires.NotNull(jwtBearerOptions, nameof(jwtBearerOptions));
            Requires.NotNull(rpSaasAuthority, nameof(rpSaasAuthority));

            services.AddVsSaaSCoreDataProtection(hostEnvironment, redisCacheOptions.RedisConnectionString);

            services.AddAuthentication()
                .AddVsSaaSJwtBearer(jwtBearerOptions)
                .AddRPSaaSJwtBearer(rpSaasAuthority)
                .AddVMTokenJwtBearer()
                .AddVsSaaSCookieBearer();

            return services;
        }

        private static void AddVsSaaSCoreDataProtection(
            this IServiceCollection services,
            IHostingEnvironment hostEnvironment,
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
