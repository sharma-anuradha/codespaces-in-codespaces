// <copyright file="GitHubServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for Billing.
    /// </summary>
    public static class GitHubServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the PartnerService as a HostedService to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="useMockCloudEnvironmentRepository">Boolean indicating the use of mocks.</param>
        /// <returns>The service instance.</returns>
        public static IServiceCollection AddGitHubWorker(
            this IServiceCollection services,
            bool useMockCloudEnvironmentRepository)
        {
            if (useMockCloudEnvironmentRepository)
            {
                services.AddSingleton<IPartnerCloudStorageFactory, MockPartnerCloudStorageFactory>();
            }
            else
            {
                services.AddSingleton<IPartnerCloudStorageFactory, PartnerCloudStorageFactory<GitHubQueueCollection>>();
            }

            services.AddSingleton<IPartnerService, GitHubService>();
            services.AddHostedService<GitHubWorker>();
            return services;
        }
    }
}
