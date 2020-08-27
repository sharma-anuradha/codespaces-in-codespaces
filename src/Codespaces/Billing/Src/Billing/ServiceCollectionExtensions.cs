// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Repository.AzureCosmosDb;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks;
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
        /// <param name="billingSettings">The billing settings.</param>
        /// <param name="billingMeterSettings">The billing meter settings.</param>
        /// <param name="useMockCloudEnvironmentRepository">A value indicating whether to use a mock repository.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddBillingEventManager(
            this IServiceCollection services,
            BillingSettings billingSettings,
            BillingMeterSettings billingMeterSettings,
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

                services.AddVsoDocumentDbCollection<BillSummary, IBillSummaryRepository, CosmosDbBillSummaryRepository>(CosmosDbBillSummaryRepository.ConfigureOptions);
                services.AddVsoDocumentDbCollection<EnvironmentStateChange, IEnvironmentStateChangeRepository, CosmosDbEnvironmentStateChangeRepository>(CosmosDbEnvironmentStateChangeRepository.ConfigureOptions);

                services.AddVsoDocumentDbCollection<BillSummary, IBillSummaryArchiveRepository, CosmosDbBillSummaryArchiveRepository>(CosmosDbBillSummaryArchiveRepository.ConfigureOptions);
                services.AddVsoDocumentDbCollection<EnvironmentStateChange, IEnvironmentStateChangeArchiveRepository, CosmosDbEnvironmentStateChangeArchiveRepository>(CosmosDbEnvironmentStateChangeArchiveRepository.ConfigureOptions);
            }

            // Job warmup
            services.AddSingleton<IAsyncBackgroundWarmup, BillingRegisterJobs>();
            services.AddSingleton<IBillingManagementConsumer, BillingManagementConsumer>();
            services.AddSingleton<IBillingManagementProducer, BillingManagementProducer>();
            services.AddSingleton<IBillingPlanBatchConsumer, BillingPlanBatchConsumer>();
            services.AddSingleton<IBillingPlanBatchProducer, BillingPlanBatchProducer>();
            services.AddSingleton<IBillingPlanSummaryConsumer, BillingPlanSummaryConsumer>();
            services.AddSingleton<IBillingPlanSummaryProducer, BillingPlanSummaryProducer>();
            services.AddSingleton<IBillingPlanCleanupProducer, BillingPlanCleanupProducer>();
            services.AddSingleton<IBillingPlanCleanupConsumer, BillingPlanCleanupConsumer>();

            // Add the Billing Meter catalog
            services.Configure<BillingMeterSettings>(
                options =>
                {
                    options.MetersByLocation = billingMeterSettings.MetersByLocation;

                    options.MetersByResource = billingMeterSettings.MetersByResource;
                });
            services.AddSingleton(billingSettings);

            services.AddSingleton<IBillingMeterCatalog, BillingMeterCatalog>();
            services.AddSingleton<IBillingEventManager, BillingEventManager>();
            services.AddSingleton<IBillSummaryGenerator, BillSummaryGenerator>();
            services.AddSingleton<IBillSummaryScrubber, BillSummaryScrubber>();
            services.AddSingleton<IBillSummaryManager, BillSummaryManager>();
            services.AddSingleton<IBillingArchivalManager, BillingArchivalManager>();
            services.AddSingleton<IEnvironmentStateChangeManager, EnvironmentStateChangeManager>();
            services.AddSingleton<IBillingMeterService, BillingMeterService>();
            services.AddSingleton<IBillingSubmissionCloudStorageFactory, BillingSubmissionCloudStorageFactory>();

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
            services.AddHostedService<BillingV2Worker>();

            services.AddSingleton<IBillingService, BillingService>();
            return services;
        }

        /// <summary>
        /// Add the <see cref="BillingEventToBillingWindowMapper"/> to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>service instance.</returns>
        public static IServiceCollection AddBillingEventToBillingWindowMapper(
            this IServiceCollection services)
        {
            services.AddSingleton<IBillingEventToBillingWindowMapper, BillingEventToBillingWindowMapper>();
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
