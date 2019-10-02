// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for Billing.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the <see cref="IBillingEventRepository"/> and <see cref="IBillingEventManager"/> to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="useMockCloudEnvironmentRepository">A value indicating whether to use a mock repository</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddBillingEventManager(
            this IServiceCollection services,
            bool useMockCloudEnvironmentRepository)
        {
            if (useMockCloudEnvironmentRepository)
            {
                services.AddSingleton<IBillingEventRepository, MockBillingEventRepository>();
            }
            else
            {
                services.AddDocumentDbCollection<BillingEvent, IBillingEventRepository, BillingEventRepository>(BillingEventRepository.ConfigureOptions);
            }

            services.AddSingleton<IBillingEventManager, BillingEventManager>();

            return services;
        }

        /// <summary>
        /// Add the <see cref="BillingService"/> and <see cref="BillingWorker"/> to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="useMockCloudEnvironmentRepository">A value indicating whether to use a mock repository.</param>
        /// <returns>service instance.</returns>
        public static IServiceCollection AddBillingWorker(
            this IServiceCollection services,
            bool useMockCloudEnvironmentRepository)
        {
            services.AddHostedService<BillingWorker>();
            services.AddSingleton<IBillingService, BillingService>();

            return services;
        }
    }
}
