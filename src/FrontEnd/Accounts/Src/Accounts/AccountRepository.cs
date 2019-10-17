// <copyright file="AccountRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Azure.Documents;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Accounts
{
    [DocumentDbCollectionId(AccountCollectionId)]
    public class AccountRepository : DocumentDbCollection<VsoAccount>, IAccountRepository
    {
        public const string AccountCollectionId = "environment_billing_accounts";

        public AccountRepository(
            IOptions<DocumentDbCollectionOptions> options,
            IDocumentDbClientProvider clientProvider,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet defaultLogValues)
            : base(
                new DocumentDbCollectionOptionsSnapshot(options, ConfigureOptions),
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
            options.PartitioningStrategy = PartitioningStrategy.Custom;
            options.CustomPartitionKeyPaths = new[]
            {
                // Partitioning on Subscription ID under the Account object
                "/account/subscription",
            };
            options.CustomPartitionKeyFunc = (entity) =>
            {
                return new PartitionKey(((VsoAccount)entity).Account?.Subscription);
            };
        }
    }
}
