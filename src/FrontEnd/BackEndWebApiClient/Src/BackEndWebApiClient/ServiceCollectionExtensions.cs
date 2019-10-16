// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.HealthMonitor;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker.Fakes;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for the user profile module.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the back end <see cref="IHttpClientProvider"/> and <see cref="IResourceBroker"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">The back end http client provider options.</param>
        /// <param name="useMocks">Indicates whether to load mock providers.</param>
        /// <param name="useFakes">Indicates whether to load fake implementation of providers.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddBackEndHttpClient(
            this IServiceCollection services,
            Action<BackEndHttpClientProviderOptions> configureOptions,
            bool useMocks,
            bool useFakes,
            string dockerImageName,
            string publishedCLIPath)
        {
            // Add the shared back end http client provider and the resource broker http client
            services.Configure(configureOptions)
                .AddSingleton<ICurrentUserHttpClientProvider<BackEndHttpClientProviderOptions>, BackEndHttpClientProvider>()
                .AddResourceBrokerClient(useMocks, useFakes, dockerImageName, publishedCLIPath)
                .AddHeartBeatClient();

            return services;
        }

        private static IServiceCollection AddResourceBrokerClient(this IServiceCollection services, bool useMocks, bool useFakes, string dockerImageName, string publishedCLIPath)
        {
            // Note: fakes will trump mocks.
            if (useFakes)
            {
                return services.AddSingleton<IResourceBrokerResourcesHttpContract, FakeResourceBrokerClient>(x => new FakeResourceBrokerClient(dockerImageName, publishedCLIPath));
            }
            else if (useMocks)
            {
                return services.AddSingleton<IResourceBrokerResourcesHttpContract, MockResourceBrokerClient>();
            }
            else
            {
                return services.AddSingleton<IResourceBrokerResourcesHttpContract, HttpResourceBrokerClient>();
            }
        }

        private static IServiceCollection AddHeartBeatClient(this IServiceCollection services)
        {
            return services.AddSingleton<IResourceHeartBeatHttpContract, HttpResourceHeartBeatClient>();
        }

    }
}
