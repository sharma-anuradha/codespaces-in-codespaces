// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.Images;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker.Fakes;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.SecretManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager;

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
        /// <param name="dockerImageName">The name of the Docker image.</param>
        /// <param name="publishedCLIPath">The path to the published CLI.</param>
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
                .AddSingleton<IHttpClientProvider<BackEndHttpClientProviderOptions>, BackEndHttpClientProvider>()
                .AddResourceBrokerClient(useMocks, useFakes, dockerImageName, publishedCLIPath)
                .AddImagesClient(useMocks, useFakes)
                .AddHeartBeatClient()
                .AddSecretManagerClient();

            return services;
        }

        private static IServiceCollection AddResourceBrokerClient(this IServiceCollection services, bool useMocks, bool useFakes, string dockerImageName, string publishedCLIPath)
        {
            // Note: fakes will trump mocks.
            if (useFakes)
            {
                return services.AddSingleton<FakeResourceBrokerClient>(x => new FakeResourceBrokerClient(dockerImageName, publishedCLIPath))
                    .AddSingleton<IResourceBrokerResourcesHttpContract>(x => x.GetRequiredService<FakeResourceBrokerClient>())
                    .AddSingleton<IResourceBrokerResourcesExtendedHttpContract>(x => x.GetRequiredService<FakeResourceBrokerClient>());
            }
            else if (useMocks)
            {
                return services.AddSingleton<MockResourceBrokerClient>()
                    .AddSingleton<IResourceBrokerResourcesHttpContract>(x => x.GetRequiredService<MockResourceBrokerClient>())
                    .AddSingleton<IResourceBrokerResourcesExtendedHttpContract>(x => x.GetRequiredService<MockResourceBrokerClient>());
            }
            else
            {
                return services.AddSingleton<HttpResourceBrokerClient>()
                    .AddSingleton<IResourceBrokerResourcesHttpContract>(x => x.GetRequiredService<HttpResourceBrokerClient>())
                    .AddSingleton<IResourceBrokerResourcesExtendedHttpContract>(x => x.GetRequiredService<HttpResourceBrokerClient>());
            }
        }

        private static IServiceCollection AddImagesClient(this IServiceCollection services, bool useMocks, bool useFakes)
        {
            // No distinction between fakes/mocks for this controller -- it just acts like no overrides are present in both cases.
            if (useFakes || useMocks)
            {
                return services.AddSingleton<IImagesHttpClient, MockImagesHttpClient>();
            }
            else
            {
                return services.AddSingleton<IImagesHttpClient, ImagesHttpClient>();
            }
        }

        private static IServiceCollection AddHeartBeatClient(this IServiceCollection services)
        {
            return services.AddSingleton<IResourceHeartBeatHttpContract, HttpResourceHeartBeatClient>();
        }

        private static IServiceCollection AddSecretManagerClient(this IServiceCollection services)
        {
            return services.AddSingleton<ISecretManagerHttpContract, SecretManagerHttpClient>();
        }
    }
}
