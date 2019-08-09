// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker;
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
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddBackEndHttpClient(
            this IServiceCollection services,
            Action<BackEndHttpClientProviderOptions> configureOptions,
            bool useMocks)
        {
            // Add the shared back end http client provider and the resource broker http client
            services.Configure(configureOptions)
                .AddSingleton<ICurrentUserHttpClientProvider<BackEndHttpClientProviderOptions>, BackEndHttpClientProvider>()
                .AddResourceBrokerClient(useMocks);

            return services;
        }

        private static void AddResourceBrokerClient(this IServiceCollection services, bool useMocks)
        {
            if (useMocks)
            {
                services.AddSingleton<IResourceBrokerHttpContract, MockResourceBrokerClient>();
            }
            else
            {
                services.AddSingleton<IResourceBrokerHttpContract, HttpResourceBrokerClient>();
            }
        }
    }
}
