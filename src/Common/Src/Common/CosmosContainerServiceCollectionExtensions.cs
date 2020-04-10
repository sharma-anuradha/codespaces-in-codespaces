// <copyright file="CosmosContainerServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Azure.Cosmos;
using Microsoft.VsSaaS.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for document db collections.
    /// </summary>
    public static class CosmosContainerServiceCollectionExtensions
    {
        /// <summary>
        /// Add a document db collection to the service collection, defaulting
        /// common options for VSO collections:
        /// <see cref="DocumentDbCollectionOptions.LogPreconditionFailedErrorsAsWarnings"/> = true.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TCollection">The collection interface type.</typeparam>
        /// <typeparam name="TImplementation">The collection implementation class.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">The configure options callback.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddVsoCosmosContainer<TEntity, TCollection, TImplementation>(
            this IServiceCollection services,
            Action<CosmosContainerOptions> configureOptions)
            where TEntity : class, IEntity, new()
            where TCollection : class, ICosmosContainer<TEntity>
            where TImplementation : class, TCollection
        {
            // Set default/common VSO options before calling the particular
            // collection configuration callback.
            void VsoConfigureOptions(CosmosContainerOptions options)
            {
                options.LogPreconditionFailedErrorsAsWarnings = true;
                configureOptions(options);
            }

            return services.AddCosmosContainer<TEntity, TCollection, TImplementation>(VsoConfigureOptions);
        }
    }
}
