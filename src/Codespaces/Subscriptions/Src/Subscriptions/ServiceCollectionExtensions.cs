// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.Subscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Subscriptions.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Subscriptions.Http;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Http;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for the Subscription Manager.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the <see cref="SubscriptionRepository"/> and <see cref="ISubscriptionRepository"/> to the service collection.
        /// </summary>
        /// <param name="services">The IServiceCollection object.</param>
        /// <param name="subscriptionManagerSettings">The subscription manager settings .</param>
        /// <param name="useMocks">A value indicating whether to use mock implementations.</param>
        /// <returns>An IServiceCollection.</returns>
        public static IServiceCollection AddSubscriptionManagers(
            this IServiceCollection services,
            SubscriptionManagerSettings subscriptionManagerSettings,
            bool useMocks)
        {
            services.AddSingleton(subscriptionManagerSettings);

            if (useMocks)
            {
                services.AddSingleton<ISubscriptionManager, MockSubscriptionManager>();
            }
            else
            {
                // Repository
                services.AddDocumentDbCollection<Subscription, ISubscriptionRepository, SubscriptionRepository>(SubscriptionRepository.ConfigureOptions);

                // Subscription manager
                services.AddSingleton<ISubscriptionManager, SubscriptionManager>();
                services.AddSingleton<ISubscriptionOfferManager, SubscriptionOfferManager>();
            }

            return services;
        }

        /// <summary>
        /// Add the set of subscription workers to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>service instance.</returns>
        public static IServiceCollection AddSubscriptionWorkers(
            this IServiceCollection services)
        {
            // Get the tasks queued up.
            services.AddSingleton<IUpdateSubscriptionDetailsTask, UpdateSubscriptionDetailsTask>();
            services.AddSingleton<IBannedSubscriptionTask, BannedSubscriptionTask>();
            services.AddSingleton<IAsyncBackgroundWarmup, SubscriptionRegisterJobs>();

            return services;
        }

        /// <summary>
        /// Adds the <see cref="RPaaSMetaRPHttpProvider{TOptions}"/>  to the service collection.
        /// </summary>
        /// <param name="services"> the service container.</param>
        /// <param name="configureOptions">RPaaS options.</param>
        /// <returns>serive container.</returns>
        public static IServiceCollection AddSubscriptionsHttpProvider(
            this IServiceCollection services,
            Action<RPaaSMetaRPOptions> configureOptions)
        {
            services.Configure(configureOptions)
                .AddSingleton<IHttpClientProvider<RPaaSMetaRPOptions>,
                    RPaaSMetaRPHttpProvider<RPaaSMetaRPOptions>>()
                .AddSingleton<IRPaaSMetaRPHttpClient, RPaasMetaRPHttpClient>();
            return services;
        }
    }
}
