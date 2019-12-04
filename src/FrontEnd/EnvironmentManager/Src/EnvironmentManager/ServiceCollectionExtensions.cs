// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repositories;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repositories.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks;

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
        /// <param name="environmentManagerSettings">Target environment manager settings.</param>
        /// <param name="useMockCloudEnvironmentRepository">A value indicating whether to use a mock repository</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddEnvironmentManager(
            this IServiceCollection services,
            EnvironmentManagerSettings environmentManagerSettings,
            bool useMockCloudEnvironmentRepository)
        {
            services.AddSingleton(environmentManagerSettings);

            if (useMockCloudEnvironmentRepository)
            {
                services.AddSingleton<ICloudEnvironmentRepository, MockCloudEnvironmentRepository>();
            }
            else
            {
                services.AddDocumentDbCollection<CloudEnvironment, ICloudEnvironmentRepository, DocumentDbCloudEnvironmentRepository>(DocumentDbCloudEnvironmentRepository.ConfigureOptions);
            }

            // The environment mangaer
            services.AddSingleton<ICloudEnvironmentManager, CloudEnvironmentManager>();

            // Register tasks
            services.AddSingleton<IWatchOrphanedSystemEnvironmentsTask, WatchOrphanedSystemEnvironmentsTask>();
            services.AddSingleton<ILogCloudEnvironmentStateTask, LogCloudEnvironmentStateTask>();

            // Job warmup
            services.AddSingleton<IAsyncBackgroundWarmup, EnvironmentRegisterJobs>();

            return services;
        }
    }
}
