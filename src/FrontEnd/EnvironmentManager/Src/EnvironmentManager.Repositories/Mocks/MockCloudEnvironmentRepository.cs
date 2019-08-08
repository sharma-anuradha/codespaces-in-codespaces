// <copyright file="MockCloudEnvironmentRepository.cs" company="Microsoft">
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

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repositories.Mocks
{
    /// <summary>
    /// An in-memory cloud environment repository.
    /// </summary>
    public class MockCloudEnvironmentRepository : ICloudEnvironmentRepository
    {
        private IDictionary<string, CloudEnvironment> Store { get; } = new Dictionary<string, CloudEnvironment>();

        /// <inheritdoc/>
        public Task<CloudEnvironment> CreateAsync(CloudEnvironment document, IDiagnosticsLogger logger)
        {
            Store.Add(document.Id, document);
            return Task.FromResult(document);
        }

        /// <inheritdoc/>
        public async Task<CloudEnvironment> CreateOrUpdateAsync(CloudEnvironment document, IDiagnosticsLogger logger)
        {
            return await (string.IsNullOrEmpty(document.Id) ?
                CreateAsync(document, logger) : UpdateAsync(document, logger));
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(DocumentDbKey key, IDiagnosticsLogger logger)
        {
            var item = await GetAsync(key, logger);
            if (item != null)
            {
                return Store.Remove(item.Id);
            }

            return false;
        }

        /// <inheritdoc/>
        public Task<CloudEnvironment> GetAsync(DocumentDbKey key, IDiagnosticsLogger logger)
        {
            return Task.FromResult(Store[key.Id]);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<CloudEnvironment>> GetWhereAsync(Expression<Func<CloudEnvironment, bool>> where, IDiagnosticsLogger logger, Func<IEnumerable<CloudEnvironment>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            return Task.FromResult(Store.Select(x => x.Value).Where(where.Compile()));
        }

        /// <inheritdoc/>
        public Task<IEnumerable<CloudEnvironment>> QueryAsync(Func<IOrderedQueryable<CloudEnvironment>, IDocumentQuery<CloudEnvironment>> queryBuilder, IDiagnosticsLogger logger, Func<IEnumerable<CloudEnvironment>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<CloudEnvironment> UpdateAsync(CloudEnvironment document, IDiagnosticsLogger logger)
        {
            await DeleteAsync(new DocumentDbKey(document.Id), logger);
            await CreateAsync(document, logger);
            return document;
        }
    }
}
