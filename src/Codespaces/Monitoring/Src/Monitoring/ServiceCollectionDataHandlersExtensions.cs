// <copyright file="ServiceCollectionDataHandlersExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Monitoring.DataHandlers;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Monitoring
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for <see cref="IDataHandler"/>.
    /// </summary>
    public static class ServiceCollectionDataHandlersExtensions
    {
        /// <summary>
        /// Add <see cref="IDataHandler"/>s for HeartBeat data processing.
        /// </summary>
        /// <param name="services">The service collection instance.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddHeartBeatDataHandlers(this IServiceCollection services)
        {
            services.AddSingleton<IDataHandler, EnvironmentDataHandler>();
            services.AddSingleton<IDataHandler, StartEnvironmentResultHandler>();
            services.AddSingleton<IDataHandler, ShutdownJobHandler>();
            services.AddSingleton<IDataHandler, EnvironmentSessionDataHandler>();
            services.AddSingleton<IDataHandler, GitChangesDataHandler>();
            services.AddSingleton<IDataHandler, ExportEnvironmentResultHandler>();
            /* Add additional handlers here as needed */

            // add heartbeat manager.
            services.AddSingleton<IEnvironmentHeartbeatManager, EnvironmentHeartbeatManager>();

            return services;
        }
    }
}
