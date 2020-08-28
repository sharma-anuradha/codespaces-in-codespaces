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
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Mocks
{
    /// <summary>
    /// Mock implementation for local development.
    /// </summary>
    public class MockResourceRepository : IResourceRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockResourceRepository"/> class.
        /// </summary>
        public MockResourceRepository()
        {
            Random = new Random();
        }

        private Random Random { get; }

        private ConcurrentDictionary<string, ResourceRecord> Store { get; }
            = new ConcurrentDictionary<string, ResourceRecord>();

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetPoolCodesForUnassignedAsync(IDiagnosticsLogger logger)
        {
            await Task.Delay(Random.Next(100, 1000));

            return Store
                .Select(x => x.Value)
                .Where(x => x.IsAssigned == false && x.IsDeleted == false)
                .Select(x => x.Id);
        }

        /// <inheritdoc/>
        public async Task<ResourceRecord> GetPoolReadyUnassignedAsync(string poolCode, IDiagnosticsLogger logger)
        {
            await Task.Delay(Random.Next(100, 1000));

            return Store
                .Select(x => x.Value)
                .Where(x => x.PoolReference.Code == poolCode
                    && x.IsAssigned == false
                    && x.IsReady == true
                    && x.IsDeleted == false)
                .FirstOrDefault();
        }

        /// <inheritdoc/>
        public async Task<ResourceRecord> GetPoolQueueRecordAsync(string poolCode, IDiagnosticsLogger logger)
        {
            await Task.Delay(Random.Next(100, 1000));

            return Store
                .Select(x => x.Value)
                .Where(x => x.PoolReference.Code == poolCode
                      && x.Type == Common.Contracts.ResourceType.PoolQueue)
                .FirstOrDefault();
        }

        /// <inheritdoc/>
        public async Task<int> GetPoolUnassignedCountAsync(string poolCode, IDiagnosticsLogger logger)
        {
            await Task.Delay(Random.Next(100, 1000));

            return Store
                .Select(x => x.Value)
                .Where(x => x.PoolReference.Code == poolCode
                    && x.IsAssigned == false
                    && x.IsDeleted == false
                    && x.ProvisioningStatus != OperationState.Failed
                    && x.ProvisioningStatus != OperationState.Cancelled)
                .Count();
        }

        /// <inheritdoc/>
        public async Task<int> GetPoolReadyUnassignedCountAsync(string poolCode, IDiagnosticsLogger logger)
        {
            await Task.Delay(Random.Next(100, 1000));

            return Store
                .Select(x => x.Value)
                .Where(x => x.PoolReference.Code == poolCode
                    && x.IsAssigned == false
                    && x.IsReady == true
                    && x.IsDeleted == false
                    && x.ProvisioningStatus != OperationState.Failed
                    && x.ProvisioningStatus != OperationState.Cancelled)
                .Count();
        }

        /// <inheritdoc/>
        public async Task<int> GetPoolUnassignedVersionCountAsync(string poolCode, string poolVersionCode, IDiagnosticsLogger logger)
        {
            await Task.Delay(Random.Next(100, 1000));

            return Store
                .Select(x => x.Value)
                .Where(x => x.PoolReference.Code == poolCode
                    && x.PoolReference.VersionCode == poolVersionCode
                    && x.IsAssigned == false
                    && x.IsDeleted == false
                    && x.ProvisioningStatus != OperationState.Failed
                    && x.ProvisioningStatus != OperationState.Cancelled)
                .Count();
        }

        /// <inheritdoc/>
        public async Task<int> GetPoolReadyUnassignedVersionCountAsync(string poolCode, string poolVersionCode, IDiagnosticsLogger logger)
        {
            await Task.Delay(Random.Next(100, 1000));

            return Store
                .Select(x => x.Value)
                .Where(x => x.PoolReference.Code == poolCode
                    && x.PoolReference.VersionCode == poolVersionCode
                    && x.IsAssigned == false
                    && x.IsReady == true
                    && x.IsDeleted == false
                    && x.ProvisioningStatus != OperationState.Failed
                    && x.ProvisioningStatus != OperationState.Cancelled)
                .Count();
        }

        /// <inheritdoc/>
        public async Task<int> GetPoolUnassignedNotVersionCountAsync(string poolCode, string poolVersionCode, IDiagnosticsLogger logger)
        {
            await Task.Delay(Random.Next(100, 1000));

            return Store
                .Select(x => x.Value)
                .Where(x => x.PoolReference.Code == poolCode
                    && x.PoolReference.VersionCode != poolVersionCode
                    && x.IsAssigned == false
                    && x.IsDeleted == false
                    && x.ProvisioningStatus != OperationState.Failed
                    && x.ProvisioningStatus != OperationState.Cancelled)
                .Count();
        }

        /// <inheritdoc/>
        public async Task<int> GetPoolReadyUnassignedNotVersionCountAsync(string poolCode, string poolVersionCode, IDiagnosticsLogger logger)
        {
            await Task.Delay(Random.Next(100, 1000));

            return Store
                .Select(x => x.Value)
                .Where(x => x.PoolReference.Code == poolCode
                    && x.PoolReference.VersionCode != poolVersionCode
                    && x.IsAssigned == false
                    && x.IsReady == true
                    && x.IsDeleted == false
                    && x.ProvisioningStatus != OperationState.Failed
                    && x.ProvisioningStatus != OperationState.Cancelled)
                .Count();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetPoolUnassignedAsync(string poolCode, int count, IDiagnosticsLogger logger)
        {
            await Task.Delay(Random.Next(100, 1000));

            return Store
                .Select(x => x.Value)
                .Where(x => x.PoolReference.Code == poolCode
                    && x.IsAssigned == false
                    && x.IsDeleted == false
                    && x.ProvisioningStatus != OperationState.Failed
                    && x.ProvisioningStatus != OperationState.Cancelled)
                .Select(x => x.Id)
                .Take(count);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetPoolUnassignedNotVersionAsync(string poolCode, string poolVersionCode, int count, IDiagnosticsLogger logger)
        {
            await Task.Delay(Random.Next(100, 1000));

            return Store
                .Select(x => x.Value)
                .Where(x => x.PoolReference.Code == poolCode
                    && x.PoolReference.VersionCode != poolVersionCode
                    && x.IsAssigned == false
                    && x.IsDeleted == false
                    && x.ProvisioningStatus != OperationState.Failed
                    && x.ProvisioningStatus != OperationState.Cancelled)
                .Select(x => x.Id)
                .Take(count);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<ResourceRecord>> GetFailedOperationAsync(string poolCode, int count, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
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

        /// <inheritdoc/>
        public Task ForEachAsync(Expression<Func<ResourceRecord, bool>> where, IDiagnosticsLogger logger, Func<ResourceRecord, IDiagnosticsLogger, Task> itemCallback, Func<IEnumerable<ResourceRecord>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task ForEachAsync<TR>(Func<IOrderedQueryable<ResourceRecord>, IQueryable<TR>> queryBuilder, IDiagnosticsLogger logger, Func<TR, IDiagnosticsLogger, Task> itemCallback, Func<IEnumerable<TR>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            throw new NotImplementedException();
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

        public Task<IEnumerable<SystemResourceCountByDimensions>> GetResourceCountByDimensionsAsync(IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<SystemResourceCountByDimensions>> GetComponentCountByDimensionsAsync(IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }
    }
}
