// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Metrics
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for the metrics sender.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the <see cref="IMetricsManager"/> to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureLoggerOptions">Options configuration callback.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddMetrics(this IServiceCollection services, Action<DefaultMetricsListenerOptions> configureLoggerOptions)
        {
            services.Configure(configureLoggerOptions);
            services.AddSingleton<IMetricsListener, DefaultMetricsListener>();
            services.AddSingleton<IMetricsManager, MetricsManager>();
            return services;
        }
    }
}
