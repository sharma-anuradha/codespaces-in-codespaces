// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.CosmosDb;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

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
        /// <param name="appSettings">System settings.</param>
        /// <returns>The service collection instance.</returns>
        public static IServiceCollection AddResourceBroker(
            this IServiceCollection serviceCollection, AppSettings appSettings)
        {
            Requires.NotNull(serviceCollection, nameof(serviceCollection));

            serviceCollection.AddSingleton<IResourceBroker, ResourceBroker>();
            serviceCollection.AddSingleton<IResourcePool, ResourcePool>();

            ConfigureDataServices(serviceCollection, appSettings);

            return serviceCollection;
        }

        private static void ConfigureDataServices(IServiceCollection serviceCollection, AppSettings appSettings)
        {
#if DEBUG
            if (appSettings.UseMocksForLocalDevelopment)
            {
                // Use the mock db if we're developing locally
                serviceCollection.AddSingleton<IResourceRepository, MockResourceRepository>();
                return;
            }
#endif

            serviceCollection.AddDocumentDbCollection<ResourceRecord, IResourceRepository, CosmosDbResourceRepository>(
                CosmosDbResourceRepository.ConfigureOptions);
        }
    }
}
