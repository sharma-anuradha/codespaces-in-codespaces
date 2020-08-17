// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.ServiceBus;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common.Clients
{
    /// <summary>
    /// Service extensions for the service bus client provider.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add service bus client provider.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddServiceBusClientProviders(this IServiceCollection services)
        {
            services.AddServiceBusClientProvider();

            AddAsyncWarmupSingeton<INewConnectionsQueueClientProvider, NewConnectionsQueueClientProvider>(services);
            AddAsyncWarmupSingeton<INewConnectionsSessionClientProvider, NewConnectionsSessionClientProvider>(services);

            AddAsyncWarmupSingeton<IEstablishedConnectionsQueueClientProvider, EstablishedConnectionsQueueClientProvider>(services);
            AddAsyncWarmupSingeton<IEstablishedConnectionsSessionClientProvider, EstablishedConnectionsSessionClientProvider>(services);

            AddAsyncWarmupSingeton<IConnectionErrorsQueueClientProvider, ConnectionErrorsQueueClientProvider>(services);
            AddAsyncWarmupSingeton<IConnectionErrorsSessionClientProvider, ConnectionErrorsSessionClientProvider>(services);

            return services;
        }

        private static IServiceCollection AddAsyncWarmupSingeton<TInterface, TImplementation>(IServiceCollection services)
            where TImplementation : class, TInterface, IAsyncWarmup
            where TInterface : class
        {
            services.AddSingleton<TImplementation>();
            services.AddSingleton<TInterface>((s) => s.GetRequiredService<TImplementation>());
            services.AddSingleton<IAsyncWarmup>((s) => s.GetRequiredService<TImplementation>());

            return services;
        }
    }
}
