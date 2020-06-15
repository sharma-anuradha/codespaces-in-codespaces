// <copyright file="ISubscriptionRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions
{
    /// <summary>
    /// Represents a repository of subscriptions.
    /// </summary>
    public interface ISubscriptionRepository : IDocumentDbCollection<Subscription>
    {
        /// <summary>
        /// Gets the set of subscriptions banned and not processed.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A list of banned subscriptions.</returns>
        Task<IEnumerable<Subscription>> GetUnprocessedBansAsync(IDiagnosticsLogger logger);
    }
}
