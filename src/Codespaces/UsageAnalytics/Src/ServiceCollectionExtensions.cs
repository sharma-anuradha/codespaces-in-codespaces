// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UsageAnalytics
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for UsageAnalytics.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the <see cref="IEnvironmentArchivalTimeCalculator"/> and <see cref="EnvironmentStatsGenerator"/> to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddEnvironmentArchivalCalculator(this IServiceCollection services)
        {
            services.AddSingleton<IEnvironmentArchivalTimeCalculator, ClusteringBasedEnvironmentArchivalTimeCalculator>();
            services.AddSingleton<EnvironmentStatsGenerator, EnvironmentStatsGenerator>();

            return services;
        }
    }
}
