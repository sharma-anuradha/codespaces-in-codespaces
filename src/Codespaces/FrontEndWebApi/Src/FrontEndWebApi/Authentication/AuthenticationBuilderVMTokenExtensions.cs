﻿// <copyright file="AuthenticationBuilderVMTokenExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.AspNetCore.Http;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Extensions;
using Microsoft.VsSaaS.Tokens;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication
{
    /// <summary>
    /// Authentication builder for VM token extensions.
    /// </summary>
    public static class AuthenticationBuilderVMTokenExtensions
    {
        /// <summary>
        /// Authentication scheme for the VM Tokens.
        /// </summary>
        public const string AuthenticationScheme = "vmtoken";

        /// <summary>
        /// Name of the key in the http context to store the "sub" from the jwt token.
        /// </summary>
        public const string VMResourceIdName = "VMResourceID";

        private const string BadTokenMessage = "vmtoken_jwt_bearer_bad_token";

        /// <summary>
        /// Adds VMToken jwt bearer authentication methods.
        /// </summary>
        /// <param name="builder">Authentication builder.</param>
        /// <returns>Authentication builder with VM Token authentication scheme.</returns>
        public static AuthenticationBuilder AddVMTokenJwtBearer(
            this AuthenticationBuilder builder)
        {
            var jwtReader = new JwtReader();

            builder
                .AddJwtBearer(AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = jwtReader.GetValidationParameters(
                        () => ApplicationServicesProvider.GetRequiredService<IDiagnosticsLogger>());

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = AuthenticationFailedAsync,
                        OnTokenValidated = TokenValidatedAsync,
                    };
                })
                .Services
                .AddTokenSettingsToJwtReader(jwtReader, (authSettings) => authSettings.VmTokenSettings);

            return builder;
        }

        private static async Task AuthenticationFailedAsync(AuthenticationFailedContext arg)
        {
            await Task.CompletedTask;

            var logger = arg.HttpContext.GetLogger() ?? new JsonStdoutLogger(new LogValueSet());

            logger
                .AddAuthenticationResultContext(arg)
                .FluentAddValue("Exception", arg.Exception.Message)
                .LogError("vmtoken_jwt_authentication_failed");
        }

        private static async Task TokenValidatedAsync(TokenValidatedContext context)
        {
            await Task.CompletedTask;

            // Locate needed services
            var logger = context.HttpContext.GetLogger() ?? new JsonStdoutLogger(new LogValueSet());

            logger.AddAuthenticationResultContext(context);

            var jwtToken = context.SecurityToken as JwtSecurityToken;
            if (string.IsNullOrEmpty(jwtToken?.RawData))
            {
                logger.LogErrorWithDetail("vmtoken_jwt_validation_error", BadTokenMessage);
                context.Fail(BadTokenMessage);
            }

            // TODO: janraj, make this a policy.
            // JWT "sub" claim gets stored in User claims as type "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
            var sub = context.Principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            context.HttpContext.Items[VMResourceIdName] = sub;

            // Subject of the JWT identifies the vm.
            // Needed for per-vm throttling to work correctly through VS SaaS SDK.
            context.HttpContext.SetCurrentUserId(sub);
        }
    }
}
