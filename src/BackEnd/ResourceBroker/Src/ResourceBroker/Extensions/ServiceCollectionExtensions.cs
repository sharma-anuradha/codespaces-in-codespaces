// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.CosmosDb;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions
{
    /// <summary>
    /// Extensions methods for <see cref="IServiceCollection"/> related to the system catalog.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the system catalog to the service collection.
        /// </summary>
        /// <param name="serviceCollection">The service collection.</param>
        /// <param name="storageAccountSettings">Settings for Azure storage access.</param>
        /// <param name="useMockedServices">Whether or not to use mocked services for external data.</param>
        /// <returns>The service collection instance.</returns>
        public static IServiceCollection AddResourceBroker(
            this IServiceCollection serviceCollection,
            StorageAccountSettings storageAccountSettings,
            bool useMockedServices)
        {
            Requires.NotNull(serviceCollection, nameof(serviceCollection));
            Requires.NotNull(storageAccountSettings, nameof(storageAccountSettings));

            serviceCollection.AddSingleton<IResourceBroker, ResourceBroker>();
            serviceCollection.AddSingleton<IResourcePool, ResourcePool>();

            ConfigureDataServices(serviceCollection, storageAccountSettings, useMockedServices);

            return serviceCollection;
        }

        private static void ConfigureDataServices(
            IServiceCollection serviceCollection,
            StorageAccountSettings storageAccountSettings,
            bool useMockedServices)
        {
            if (useMockedServices)
            {
                // Use the mock db if we're developing locally
                serviceCollection.AddSingleton<IResourceRepository, MockResourceRepository>();
                return;
            }

            serviceCollection.AddDocumentDbCollection<ResourceRecord, IResourceRepository, CosmosDbResourceRepository>(
                CosmosDbResourceRepository.ConfigureOptions);

            // TODO: mock storage queue for local testing
            serviceCollection.Configure<StorageAccountOptions>(x => x.Settings = storageAccountSettings);
            serviceCollection.AddSingleton<IStorageQueueClientProvider, StorageQueueClientProvider>();
        }
    }
}
