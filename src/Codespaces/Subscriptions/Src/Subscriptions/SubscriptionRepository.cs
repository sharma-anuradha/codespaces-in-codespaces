// <copyright file="SubscriptionRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions
{
    /// <summary>
    /// The banned subscriptions repository.
    /// </summary>
    [DocumentDbCollectionId(SubscriptionCollectionId)]
    public class SubscriptionRepository : DocumentDbCollection<Subscription>, ISubscriptionRepository
    {
        /// <summary>
        /// The subscriptions collection id.
        /// </summary>
        public const string SubscriptionCollectionId = "subscriptions";

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionRepository"/> class.
        /// </summary>
        /// <param name="collectionOptions">The container options.</param>
        /// <param name="clientProvider">The doc db client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The diagnostics logging factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public SubscriptionRepository(
            IOptionsMonitor<DocumentDbCollectionOptions> collectionOptions,
            IDocumentDbClientProvider clientProvider,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet defaultLogValues)
            : base(
                collectionOptions,
                clientProvider,
                healthProvider,
                loggerFactory,
                defaultLogValues)
        {
        }

        /// <summary>
        /// Configures the standard options for this repository.
        /// </summary>
        /// <param name="options">The options instance.</param>
        public static void ConfigureOptions(DocumentDbCollectionOptions options)
        {
            Requires.NotNull(options, nameof(options));
            options.PartitioningStrategy = PartitioningStrategy.IdOnly;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Subscription>> GetUnprocessedBansAsync(IDiagnosticsLogger logger)
        {
            var query = new SqlQuerySpec(
                @"SELECT *
                FROM c
                WHERE c.bannedReason > 0 AND (c.banComplete != true or not IS_DEFINED(c.banComplete))");

            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<Subscription>(uri, query, feedOptions).AsDocumentQuery(), logger.NewChildLogger());
            return items;
        }
    }
}
