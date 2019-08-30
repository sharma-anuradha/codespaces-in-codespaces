// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repositories;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repositories.Mocks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for the Environment Manager.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the <see cref="CloudEnvironmentRepository"/> and <see cref="ICloudEnvironmentManager"/> to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureSessionSettings">Configure the session settings.</param>
        /// <param name="useMockCloudEnvironmentRepository">A value indicating whether to use a mock repository (DEBUG only).</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddEnvironmentManager(
            this IServiceCollection services,
            bool useMockCloudEnvironmentRepository)
        {
            _ = useMockCloudEnvironmentRepository;

#if DEBUG
            if (useMockCloudEnvironmentRepository)
            {
                services.AddSingleton<ICloudEnvironmentRepository, MockCloudEnvironmentRepository>();
            }
            else
#endif
            {
                services.AddDocumentDbCollection<CloudEnvironment, ICloudEnvironmentRepository, DocumentDbCloudEnvironmentRepository>(DocumentDbCloudEnvironmentRepository.ConfigureOptions);
            }

            // The environment mangaer
            services.AddSingleton<ICloudEnvironmentManager, CloudEnvironmentManager>();

            return services;
        }
    }
}
