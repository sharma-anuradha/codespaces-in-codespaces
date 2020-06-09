// <copyright file="NgrokAspNetCoreExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore.Services;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Ngrok;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore.Extensions
{
    /// <summary>
    /// Ngrok Asp.NET Core Extensions.
    /// </summary>
    public static class NgrokAspNetCoreExtensions
    {
        /// <summary>
        /// Adds Ngrok Service.
        /// </summary>
        /// <param name="services">Service Collection.</param>
        /// <param name="action">The Action.</param>
        /// <returns>An IServiceCollection.</returns>
        public static IServiceCollection AddNgrok(this IServiceCollection services, Action<NgrokOptions> action = null)
        {
            var optBuilder = ConfigureOptionsBuilder(services, action);

            services.TryAddSingleton<NgrokProcessManager>();
            services.AddHttpClient<NgrokHttpClient>();

            services.AddLogging();

            services.AddSingleton<NgrokHostedService>();
            services.AddSingleton<IHostedService>(p => p.GetRequiredService<NgrokHostedService>());

            return services;
        }

        private static OptionsBuilder<NgrokOptions> ConfigureOptionsBuilder(IServiceCollection services, Action<NgrokOptions> action = null)
        {
            var optBuilder = services.AddOptions<NgrokOptions>();
            if (action != null)
            {
                optBuilder.Configure(action);
            }

            optBuilder.PostConfigure(PostConfigure);
            optBuilder.Validate(ValidateUrlDetectOpt, "Must supply an ApplicationHttpUrl if DetectUrl is false");
            return optBuilder;
        }

        private static void PostConfigure(NgrokOptions opt)
        {
        }

        private static bool ValidateUrlDetectOpt(NgrokOptions opt)
        {
            if (opt.DetectUrl == false && string.IsNullOrWhiteSpace(opt.ApplicationHttpUrl))
            {
                return false;
            }

            return true;
        }
    }
}
