// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Services.CloudEnvironments.ScalingEngine.Jobs;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ScalingEngine.Extensions
{
    /// <summary>
    /// Extensions methods for <see cref="IServiceCollection"/> related to the system catalog.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// <see cref="IServiceCollection"/> extensions for the scaling engine.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddScalingEngine(
            this IServiceCollection services)
        {
            Requires.NotNull(services, nameof(services));

            services.AddSingleton<IAsyncWarmup, RefreshPoolScaleTargetsJob>();
            services.AddDocumentDbCollection<ResourcePoolSettingsRecord, IResourcePoolSettingsRepository, CosmosDbResourcePoolSettingsRepository>(
                CosmosDbResourcePoolSettingsRepository.ConfigureOptions);

            return services;
        }
    }
}
