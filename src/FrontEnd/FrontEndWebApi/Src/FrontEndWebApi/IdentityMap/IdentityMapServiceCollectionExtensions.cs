// <copyright file="IdentityMapServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.IdentityMap
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for identity map.
    /// </summary>
    public static class IdentityMapServiceCollectionExtensions
    {
        /// <summary>
        /// Add support for identity map.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddIdentityMap(this IServiceCollection services)
        {
            services.AddDocumentDbCollection<IdentityMapEntity, IIdentityMapRepository, IdentityMapRepository>(IdentityMapRepository.ConfigureOptions);
            return services;
        }
    }
}
