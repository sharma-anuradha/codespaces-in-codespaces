// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ScalingEngine.Jobs;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ScalingEngine.Extensions
{
    /// <summary>
    /// Extensions methods for <see cref="IServiceCollection"/> related to the system catalog.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="appSettings"></param>
        /// <returns></returns>
        public static IServiceCollection AddScalingEngine(
            this IServiceCollection services,
            AppSettings appSettings)
        {
            Requires.NotNull(services, nameof(services));
            Requires.NotNull(appSettings, nameof(appSettings));

            services.AddSingleton<IAsyncWarmup, InitializeScaleLevelCache>();

            return services;
        }
    }
}
