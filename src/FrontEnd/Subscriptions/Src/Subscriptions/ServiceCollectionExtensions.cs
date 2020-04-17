// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for the Subscription Manager.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the <see cref="BannedSubscriptionRepository"/> and <see cref="IBannedSubscriptionRepository"/> to the service collection.
        /// </summary>
        /// <param name="services">The IServiceCollection object.</param>
        /// <param name="options">The subscription manager settings .</param>
        /// <param name="useMocks">A value indicating whether to use mock implementations.</param>
        /// <returns>An IServiceCollection.</returns>
        public static IServiceCollection AddSubscriptionManager(
            this IServiceCollection services,
            Action<SubscriptionManagerSettings> options,
            bool useMocks)
        {
            if (useMocks)
            {
                services.AddSingleton<ISubscriptionManager, MockSubscriptionManager>();
            }
            else
            {
                // Repository
                services.AddVsoCosmosContainer<BannedSubscription, IBannedSubscriptionRepository, BannedSubscriptionRepository>(BannedSubscriptionRepository.ConfigureOptions);

                // Subscription manager
                services.Configure(options);
                services.AddSingleton<ISubscriptionManager, SubscriptionManager>();
            }

            return services;
        }

        /// <summary>
        /// Add the <see cref="BannedSubscriptionsWorker"/> to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>service instance.</returns>
        public static IServiceCollection AddSubscriptionWorkers(
            this IServiceCollection services)
        {
            // Banned subscriptions worker
            services.AddHostedService<BannedSubscriptionsWorker>();
            return services;
        }
    }
}
