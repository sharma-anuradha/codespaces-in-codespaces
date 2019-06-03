//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsCloudKernel.Services.EnvReg.Models;
using StackExchange.Redis;

namespace Microsoft.VsCloudKernel.Services.EnvReg.WebApi.Authentication
{
    public static class AuthenticationServiceCollectionExtensions
    {
        private const string ProxySchema = "Proxy";
        private const string DataProtectionName = "VsClkService";
        private const string DataProtectionRedisKey = "DataProtection-Keys";

        public static void AddVsClkCoreAuthenticationServices(
            this IServiceCollection services,
            IHostingEnvironment hostEnvironment,
            AppSettings appSettings)
        {
            services.AddVsClkCoreDataProtection(hostEnvironment, appSettings);

            services
                .AddAuthentication(ProxySchema)
                .AddPolicyScheme(ProxySchema, "Authorization Bearer or Cookie", options =>
                {
                    options.ForwardDefaultSelector = context =>
                    {
                        // Dynamically switch to the correct underlying scheme based on path
                        if (context.Request.Path.HasValue 
                            && context.Request.Path.Value.Contains("/api/"))
                        {
                            return AuthenticationBuilderVsClkJwtExtension.AuthenticationScheme;
                        }
                        return AuthenticationBuilderVsClkCookieExtension.AuthenticationScheme;
                    };
                })
                .AddVsClkJwtBearer(appSettings)
                .AddVsClkCookieBearer(appSettings);
        }

        private static void AddVsClkCoreDataProtection(
            this IServiceCollection services,
            IHostingEnvironment hostEnvironment,
            AppSettings appSettings)
        {
            var dataProtection = services
                .AddDataProtection()
                .SetApplicationName(DataProtectionName);

            if (!hostEnvironment.IsDevelopment() || !appSettings.IsLocal)
            {
                // TODO: Need to make sure that keys are persisted, etc
                var redis = ConnectionMultiplexer.Connect(appSettings.VsClkRedisConnectionString);
                dataProtection.PersistKeysToStackExchangeRedis(redis, DataProtectionRedisKey);
            }
        }
    }
}
