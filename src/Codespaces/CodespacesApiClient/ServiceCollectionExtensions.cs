// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.CodespacesApiClient
{
    /// <summary>
    /// Provides client for accessing FrontEnd service APIs.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the <see cref="ICodespacesApiClient"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">The front end client options.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IHttpClientBuilder AddHttpCodespacesApiClient(
            this IServiceCollection services,
            Action<HttpCodespacesApiClientOptions> configureOptions)
        {
            services.AddTransient<ForwardingCorrelationIdHandler>();

            return services.Configure(configureOptions)
                .AddHttpClient<ICodespacesApiClient, HttpCodespacesApiClient>()
                .AddHttpMessageHandler<ForwardingCorrelationIdHandler>()
                .ConfigurePrimaryHttpMessageHandler(_ => new SocketsHttpHandler()
                {
                    AllowAutoRedirect = false,
                });
        }
    }
}
