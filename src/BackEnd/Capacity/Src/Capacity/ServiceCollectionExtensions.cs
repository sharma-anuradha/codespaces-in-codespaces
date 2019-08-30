// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Mocks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for the capacity manager.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the capacity manager to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="useMocksForExternalDependencies">A value indicating whether to use mocks.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddCapacityManager(this IServiceCollection services, bool useMocksForExternalDependencies)
        {
            if (useMocksForExternalDependencies)
            {
                services.AddSingleton<ICapacityManager, MockCapacityManager>();
            }
            else
            {
                services.AddSingleton<ICapacityManager, CapacityManager>();
            }

            return services;
        }
    }
}
