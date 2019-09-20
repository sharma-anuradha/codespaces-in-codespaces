// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

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
        /// <param name="develperPersonalStamp">True to set developer personal stamp.</param>
        /// <param name="mocksSettings">The mocks settings.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddCapacityManager(
            this IServiceCollection services,
            bool develperPersonalStamp,
            MocksSettings mocksSettings = null)
        {
            if (!develperPersonalStamp)
            {
                services.AddSingleton(new CapacitySettings());
            }
            else
            {
                services.AddSingleton(CapacitySettings.CreateDeveloperCapacitySettings());
            }

            if (mocksSettings?.UseMocksForExternalDependencies == true)
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
