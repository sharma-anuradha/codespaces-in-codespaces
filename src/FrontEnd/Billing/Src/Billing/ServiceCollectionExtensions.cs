// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

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
        /// <param name="useMockCloudEnvironmentRepository">A value indicating whether to use a mock repository.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddBillingEventManager(
            this IServiceCollection services,
            bool useMockCloudEnvironmentRepository)
        {
            if (useMockCloudEnvironmentRepository)
            {
                services.AddSingleton<IBillingEventRepository, MockBillingEventRepository>();
                services.AddSingleton<IBillingOverrideRepository, MockBillingOverrideRepository>();
            }
            else
            {
                services.AddVsoDocumentDbCollection<BillingEvent, IBillingEventRepository, BillingEventRepository>(BillingEventRepository.ConfigureOptions);
                services.AddVsoDocumentDbCollection<BillingOverride, IBillingOverrideRepository, BillingOverrideRepository>(BillingOverrideRepository.ConfigureOptions);
            }

            services.AddSingleton<IBillingEventManager, BillingEventManager>();

            return services;
        }

        /// <summary>
        /// Add the <see cref="BillingService"/> and <see cref="BillingWorker"/> to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>service instance.</returns>
        public static IServiceCollection AddBillingWorker(
            this IServiceCollection services)
        {
            services.AddHostedService<BillingWorker>();
            services.AddSingleton<IBillingService, BillingService>();
            return services;
        }

        /// <summary>
        /// Adds the BillingSubmissionService as a HostedService to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="useMockCloudEnvironmentRepository">Boolean indicating the use of mocks.</param>
        /// <returns>The service instance.</returns>
        public static IServiceCollection AddBillingSubmissionWorker(
            this IServiceCollection services,
            bool useMockCloudEnvironmentRepository)
        {
            if (useMockCloudEnvironmentRepository)
            {
                services.AddSingleton<IBillingSubmissionCloudStorageFactory, MockBillingSubmissionCloudStorageFactory>();
            }
            else
            {
                services.AddSingleton<IBillingSubmissionCloudStorageFactory, BillingSubmissionCloudStorageFactory>();
            }

            services.AddSingleton<IBillingSummarySubmissionService, BillingSummarySubmissionService>();
            services.AddHostedService<BillingSummarySubmissionWorker>();
            return services;
        }
    }
}
