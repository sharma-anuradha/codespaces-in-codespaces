// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for the Environment Manager.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the Live Share workspace provider.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureWorkspaceHttpClientProviderOptions">Configure the workspace options.</param>
        /// <param name="useMocks">A value indicating whether to use a mock workspace repository.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddWorkspaceProvider(
            this IServiceCollection services,
            Action<WorkspaceHttpClientProviderOptions> configureWorkspaceHttpClientProviderOptions,
            bool useMocks)
        {
            services
                .Configure(configureWorkspaceHttpClientProviderOptions)
                .AddSingleton<ICurrentUserHttpClientProvider<WorkspaceHttpClientProviderOptions>, WorkspaceHttpClientProvider>();

            if (useMocks)
            {
                services.AddSingleton<IWorkspaceRepository, MockClientWorkspaceRepository>();
            }
            else
            {
                services.AddSingleton<IWorkspaceRepository, HttpClientWorkspaceRepository>();
            }

            return services;
        }
    }
}
