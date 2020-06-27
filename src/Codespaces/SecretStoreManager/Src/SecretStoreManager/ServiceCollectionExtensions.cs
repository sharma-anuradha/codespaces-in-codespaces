// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager.Repository;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for the Secret store manager.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// DI for secret manager.
        /// </summary>
        /// <param name="services">The IServiceCollection object.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddSecretStoreManager(this IServiceCollection services)
        {
            services.AddSingleton<ISecretStoreManager, SecretStoreManager>();
            services.AddVsoDocumentDbCollection<SecretStore, ISecretStoreRepository, SecretStoreRepository>(SecretStoreRepository.ConfigureOptions);

            return services;
        }
    }
}
