// <copyright file="AuthenticationServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for authentication.
    /// </summary>
    public static class AuthenticationServiceCollectionExtensions
    {
        private const string ProxySchema = "Proxy";
        private const string DataProtectionName = ServiceConstants.ServiceName;
        private const string DataProtectionRedisKey = "DataProtection-Keys";

        /// <summary>
        /// Add Authentication.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="hostEnvironment">The aspnet host environment.</param>
        /// <param name="appSettings">The global app settings.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddVsSaaSAuthentication(
            this IServiceCollection services,
            IHostingEnvironment hostEnvironment,
            AppSettings appSettings)
        {
            services.AddVsSaaSCoreDataProtection(hostEnvironment, appSettings.RedisConnectionString);

            services
                .AddAuthentication(ProxySchema)
                .AddPolicyScheme(ProxySchema, "Authorization Bearer or Cookie", options =>
                {
                    options.ForwardDefaultSelector = context =>
                    {
                        // Dynamically switch to the correct underlying scheme based on path
                        if (context.Request.Path.HasValue &&
                            context.Request.Path.Value.Contains("/api/"))
                        {
                            return AuthenticationBuilderJwtExtensions.AuthenticationScheme;
                        }

                        return AuthenticationBuilderCookieExtensions.AuthenticationScheme;
                    };
                })
                .AddVsSaaSJwtBearer(appSettings)
                .AddVsSaaSCookieBearer(appSettings);

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
