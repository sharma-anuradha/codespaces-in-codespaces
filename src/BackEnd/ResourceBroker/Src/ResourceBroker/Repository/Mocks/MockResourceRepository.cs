// <copyright file="MockResourceRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Mocks
{
    /// <summary>
    /// Mock implementation for local development.
    /// </summary>
    public class MockResourceRepository : IResourceRepository
    {
        /// <summary>
        /// 
        /// </summary>
        public MockResourceRepository()
        {
            Random = new Random();
        }

        private Random Random { get; }

        private ConcurrentDictionary<string, ResourceRecord> Store { get; }
            = new ConcurrentDictionary<string, ResourceRecord>();

        /// <inheritdoc/>
        public async Task<ResourceRecord> GetUnassignedResourceAsync(string skuName, ResourceType type, string location, IDiagnosticsLogger logger)
        {
            await Task.Delay(Random.Next(100, 1000));

            var items = Store
                .Select(x => x.Value)
                .Where(x => x.SkuName == skuName
                    && x.Type == type
                    && x.Location == location
                    && x.IsAssigned == false)
                .OrderBy(x => x.Ready);

            return items.FirstOrDefault();
        }

        /// <inheritdoc/>
        public async Task<int> GetUnassignedCountAsync(string skuName, ResourceType type, string location, IDiagnosticsLogger logger)
        {
            await Task.Delay(Random.Next(100, 1000));

            return Store
                .Select(x => x.Value)
                .Where(x => x.SkuName == skuName
                    && x.Type == type
                    && x.Location == location
                    && x.IsAssigned == false)
                .Count();
        }

        /// <inheritdoc/>
        public Task<IEnumerable<ResourceRecord>> GetWhereAsync(Expression<Func<ResourceRecord, bool>> where, IDiagnosticsLogger logger, Func<IEnumerable<ResourceRecord>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IEnumerable<TR>> QueryAsync<TR>(Func<IOrderedQueryable<ResourceRecord>, IQueryable<TR>> queryBuilder, IDiagnosticsLogger logger, Func<IEnumerable<TR>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IEnumerable<TR>> QueryAsync<TR>(Func<IDocumentClient, Uri, FeedOptions, IDocumentQuery<TR>> queryBuilder, IDiagnosticsLogger logger, Func<IEnumerable<TR>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<ResourceRecord> UpdateAsync(ResourceRecord document, IDiagnosticsLogger logger)
        {
            await Task.Delay(Random.Next(100, 1000));

            return Update(document);
        }

        /// <inheritdoc/>
        public async Task<ResourceRecord> CreateAsync(ResourceRecord document, IDiagnosticsLogger logger)
        {
            await Task.Delay(Random.Next(100, 1000));

            return Create(document);
        }

        /// <inheritdoc/>
        public async Task<ResourceRecord> CreateOrUpdateAsync(ResourceRecord document, IDiagnosticsLogger logger)
        {
            await Task.Delay(Random.Next(100, 1000));

            return Update(document);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(DocumentDbKey key, IDiagnosticsLogger logger)
        {
            await Task.Delay(Random.Next(100, 1000));

            return Delete(key);
        }

        /// <inheritdoc/>
        public async Task<ResourceRecord> GetAsync(DocumentDbKey key, IDiagnosticsLogger logger)
        {
            await Task.Delay(Random.Next(100, 1000));

            return Get(key);
        }

        private ResourceRecord Get(DocumentDbKey key)
        {
            Store.TryGetValue(key.Id, out var resource);
            return resource;
        }

        private bool Delete(DocumentDbKey key)
        {
            return Store.TryRemove(key.Id, out var resource);
        }

        private ResourceRecord Update(ResourceRecord document)
        {
            Delete(document.Id);
            Create(document);
            return document;
        }

        private ResourceRecord Create(ResourceRecord document)
        {
            Store.TryAdd(document.Id, document);
            return document;
        }

        public Task<ResourceRecord> GetByResourceId(string resourceId, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }
    }
}
