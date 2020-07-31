// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler
{
    /// <summary>
    /// Service extensions for the job scheduler.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the job scheduler to DI.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddJobScheduler(this IServiceCollection services)
        {
            services.AddSingleton<IJobScheduler, JobScheduler>();
            services.AddSingleton<IHostedService>(srvcProvider => srvcProvider.GetRequiredService<IJobScheduler>() as IHostedService);
            return services;
        }
    }
}
