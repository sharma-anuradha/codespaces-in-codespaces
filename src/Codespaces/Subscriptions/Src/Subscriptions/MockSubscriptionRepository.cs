// <copyright file="MockSubscriptionRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Linq;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Subscriptions
{
    /// <summary>
    /// Mock Subscription Repository.
    /// </summary>
    public class MockSubscriptionRepository : MockRepository<Subscription>, ISubscriptionRepository
    {
        /// <inheritdoc/>
        public async Task<IEnumerable<Subscription>> GetUnprocessedBansAsync(IDiagnosticsLogger logger)
        {
            var items = await GetWhereAsync(item => item.BannedReason > 0 && !item.BanComplete, logger.NewChildLogger());
            return items;
        }
    }
}