// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs
{
    /// <summary>
    /// Service extensions for the job queues.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the job queue consumer telemetry factory.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddJobQueueConsumerTelemetrySummary(this IServiceCollection services)
        {
            services.AddSingleton<IAsyncBackgroundWarmup, JobQueueConsumerFactoryTelemetry>();
            return services;
        }
    }
}
