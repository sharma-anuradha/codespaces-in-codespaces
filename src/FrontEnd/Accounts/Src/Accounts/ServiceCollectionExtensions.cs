// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Accounts
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for the Account Manager.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the <see cref="AccountRepository"/> and <see cref="IAccountManager"/> to the service collection.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="useMockAccountRepository"></param>
        /// <returns></returns>
        public static IServiceCollection AddAccountManager(this IServiceCollection services, bool useMockAccountRepository)
        {
            _ = useMockAccountRepository;

            if (useMockAccountRepository)
            {
                services.AddSingleton<IAccountRepository, MockAccountRepository>();
            }
            else
            {
                services.AddDocumentDbCollection<VsoAccount, IAccountRepository, AccountRepository>(AccountRepository.ConfigureOptions);
            }

            // The Account mangaer
            services.AddSingleton<IAccountManager, AccountManager>();

            return services;
        }
    }
}
