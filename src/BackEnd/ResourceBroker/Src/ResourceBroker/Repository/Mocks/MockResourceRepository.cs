// <copyright file="MockResourceRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
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
        private IDictionary<string, IList<ResourceRecord>> Store { get; }
            = new Dictionary<string, IList<ResourceRecord>>();

        /// <inheritdoc/>
        public async Task<ResourceRecord> GetUnassignedResourceAsync(string skuName, ResourceType type, string location, IDiagnosticsLogger logger)
        {
            var key = BuildKey(skuName, type, location);
            if (Store.TryGetValue(key, out var collection))
            {
                var items = collection
                    .Where(x => x.IsAssigned == false)
                    .OrderBy(x => x.Created);

                return items.FirstOrDefault();
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<ResourceRecord> UpdateAsync(ResourceRecord record, IDiagnosticsLogger logger)
        {
            var key = BuildKey(record.SkuName, record.Type, record.Location);
            if (Store.TryGetValue(key, out var collection))
            {
                collection.Remove(record);
                collection.Add(record);

                return record;
            }

            return null;
        }

        /// <inheritdoc/>
        public Task<IEnumerable<ResourceRecord>> GetWhereAsync(Expression<Func<ResourceRecord, bool>> where, IDiagnosticsLogger logger, Func<IEnumerable<ResourceRecord>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IEnumerable<ResourceRecord>> QueryAsync(Func<IOrderedQueryable<ResourceRecord>, IDocumentQuery<ResourceRecord>> queryBuilder, IDiagnosticsLogger logger, Func<IEnumerable<ResourceRecord>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<ResourceRecord> CreateAsync(ResourceRecord document, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<ResourceRecord> CreateOrUpdateAsync(ResourceRecord document, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(DocumentDbKey key, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<ResourceRecord> GetAsync(DocumentDbKey key, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        private string BuildKey(string skuName, ResourceType type, string location)
        {
            return $"{skuName}_{type}_{location}";
        }
    }
}
