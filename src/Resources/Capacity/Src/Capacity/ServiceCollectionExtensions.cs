﻿// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VsSaaS.Caching;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

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
        /// <param name="developerPersonalStamp">True to set developer personal stamp.</param>
        /// <param name="mocksSettings">The mocks settings.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddCapacityManager(
            this IServiceCollection services,
            bool developerPersonalStamp,
            MocksSettings mocksSettings = null)
        {
            // Capacity Settings
            if (!developerPersonalStamp)
            {
                services.AddSingleton(new CapacitySettings());
            }
            else
            {
                services.AddSingleton(CapacitySettings.CreateDeveloperCapacitySettings());
            }

            // Providers and Repositories
            if (mocksSettings?.UseMocksForExternalDependencies == true)
            {
                services.AddSingleton<IAzureSubscriptionCapacityProvider, MockAzureSubscriptionCapacityProvider>();
                services.AddSingleton<ICapacityRepository, MockCapacityRepository>();
            }
            else
            {
                services.AddSingleton<IAzureSubscriptionCapacityProvider, AzureSubscriptionCapacityProvider>();
                services.AddVsoDocumentDbCollection<CapacityRecord, ICapacityRepository, CachedDocumentDbCapacityRepository>(
                    CachedDocumentDbCapacityRepository.ConfigureOptions);
                services.TryAddSingleton<IManagedCache, InMemoryManagedCache>();
            }

            // Services
            services.AddSingleton<ICapacityManager, CapacityManager>();

            // Jobs
            services.AddSingleton<IAsyncBackgroundWarmup, CapacityManagerJobs>();

            return services;
        }
    }
}
