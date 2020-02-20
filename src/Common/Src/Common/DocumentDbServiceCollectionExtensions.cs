// <copyright file="DocumentDbServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for document db collections.
    /// </summary>
    public static class DocumentDbServiceCollectionExtensions
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
        public static IServiceCollection AddVsoDocumentDbCollection<TEntity, TCollection, TImplementation>(
            this IServiceCollection services,
            Action<DocumentDbCollectionOptions> configureOptions)
            where TEntity : class, IEntity
            where TCollection : class, IDocumentDbCollection<TEntity>
            where TImplementation : class, TCollection
        {
            // Set default/common VSO options before calling the particular
            // collection configuration callback.
            void VsoConfigureOptions(DocumentDbCollectionOptions options)
            {
                options.LogPreconditionFailedErrorsAsWarnings = true;
                configureOptions(options);
            }

            return services.AddDocumentDbCollection<TEntity, TCollection, TImplementation>(VsoConfigureOptions);
        }
    }
}
